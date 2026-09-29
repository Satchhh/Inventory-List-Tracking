using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using InventoryApp.Models;

namespace InventoryApp.Services
{
    /// <summary>
    /// Result of checking a new item against what's already in the sheet.
    /// </summary>
    public class DuplicateCheckResult
    {
        public bool IsDuplicateArticle { get; set; }
        public bool IsDuplicateSerial { get; set; }
        public InventoryItem? ExistingArticleMatch { get; set; }
        public InventoryItem? ExistingSerialMatch { get; set; }

        public bool HasAnyDuplicate => IsDuplicateArticle || IsDuplicateSerial;
    }

    /// <summary>
    /// All reading and writing of the Excel file goes through this one
    /// class. Keeping it separate from the UI means the storage can be
    /// swapped later (e.g. for SQLite) without touching MainWindow.
    /// </summary>
    public class ExcelStore
    {
        private readonly string _filePath;
        private const string SheetName = "Inventory list";

        // Column order written to the sheet. Keep this in sync with
        // Append() below - add a new entry here AND a new line there
        // when a new field is added to InventoryItem.
        private static readonly string[] Headers =
        {
            "No", "Serial Number", "Article Number", "Type / Model", "Category",
            "Description", "Country of Manufacture", "Supplier", "Location",
            "Remarks", "Date Added"
        };

        public ExcelStore(string filePath)
        {
            _filePath = filePath;
        }

        /// <summary>
        /// Loads every existing row so the app can check for duplicates
        /// and show "recent entries". Called once at startup and again
        /// after every save, since the file is the single source of truth.
        /// </summary>
        public List<InventoryItem> LoadAll()
        {
            var items = new List<InventoryItem>();

            if (!File.Exists(_filePath))
                return items;

            using var workbook = new XLWorkbook(_filePath);
            if (!workbook.Worksheets.Contains(SheetName))
                return items;

            var ws = workbook.Worksheet(SheetName);
            var lastRow = ws.LastRowUsed()?.RowNumber() ?? 1;

            for (int row = 2; row <= lastRow; row++)
            {
                var serial = ws.Cell(row, 2).GetString();
                var article = ws.Cell(row, 3).GetString();

                // Skip fully blank rows (e.g. trailing empty rows in the sheet)
                if (string.IsNullOrWhiteSpace(serial) && string.IsNullOrWhiteSpace(article))
                    continue;

                items.Add(new InventoryItem
                {
                    SerialNumber = serial,
                    NoSerialNumber = string.Equals(serial, "UNKNOWN", StringComparison.OrdinalIgnoreCase)
                                      || string.IsNullOrWhiteSpace(serial),
                    ArticleNumber = article,
                    TypeModel = ws.Cell(row, 4).GetString(),
                    Category = ws.Cell(row, 5).GetString(),
                    Description = ws.Cell(row, 6).GetString(),
                    CountryOfManufacture = ws.Cell(row, 7).GetString(),
                    Supplier = ws.Cell(row, 8).GetString(),
                    Location = ws.Cell(row, 9).GetString(),
                    Remarks = ws.Cell(row, 10).GetString(),
                });
            }

            return items;
        }

        /// <summary>
        /// Checks a candidate item against everything currently in the
        /// sheet. Article-number duplicates are expected and normal
        /// (many units share one article number); serial-number
        /// duplicates are the ones worth warning about.
        /// </summary>
        public DuplicateCheckResult CheckForDuplicates(InventoryItem candidate, List<InventoryItem> existing)
        {
            var result = new DuplicateCheckResult();

            if (!candidate.NoSerialNumber && !string.IsNullOrWhiteSpace(candidate.SerialNumber))
            {
                var serialMatch = existing.FirstOrDefault(i =>
                    !i.NoSerialNumber &&
                    i.NormalizedSerialNumber == candidate.NormalizedSerialNumber);

                if (serialMatch != null)
                {
                    result.IsDuplicateSerial = true;
                    result.ExistingSerialMatch = serialMatch;
                }
            }

            return result;
        }

        /// <summary>
        /// Appends one new row and saves. Retries a few times if the
        /// file is locked (e.g. someone has it open in Excel), since
        /// that is the most common save failure on a shared file.
        /// </summary>
        public void Append(InventoryItem item)
        {
            const int maxAttempts = 3;
            Exception? lastError = null;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    AppendOnce(item);
                    return;
                }
                catch (IOException ex)
                {
                    lastError = ex;
                    System.Threading.Thread.Sleep(500);
                }
            }

            throw new InvalidOperationException(
                "Could not save to the Excel file. It may be open in Excel - " +
                "please close it and try again.", lastError);
        }

        private void AppendOnce(InventoryItem item)
        {
            XLWorkbook workbook;
            IXLWorksheet ws;

            if (File.Exists(_filePath))
            {
                workbook = new XLWorkbook(_filePath);
                ws = workbook.Worksheets.Contains(SheetName)
                    ? workbook.Worksheet(SheetName)
                    : workbook.Worksheets.Add(SheetName);
            }
            else
            {
                workbook = new XLWorkbook();
                ws = workbook.Worksheets.Add(SheetName);
            }

            // Write headers if this is a new/empty sheet.
            if (ws.Cell(1, 1).IsEmpty())
            {
                for (int i = 0; i < Headers.Length; i++)
                    ws.Cell(1, i + 1).Value = Headers[i];
                ws.Row(1).Style.Font.Bold = true;
            }

            int nextRow = (ws.LastRowUsed()?.RowNumber() ?? 1) + 1;
            int nextNo = nextRow - 1;

            // Serial/Article/Type kept as text so Excel never eats a
            // leading zero or turns a long number into 1.23E+12.
            ws.Cell(nextRow, 1).Value = nextNo;
            ws.Cell(nextRow, 2).SetValue(item.NoSerialNumber ? "UNKNOWN" : item.SerialNumber).Style.NumberFormat.SetFormat("@");
            ws.Cell(nextRow, 3).SetValue(item.ArticleNumber).Style.NumberFormat.SetFormat("@");
            ws.Cell(nextRow, 4).Value = item.TypeModel;
            ws.Cell(nextRow, 5).Value = item.Category;
            ws.Cell(nextRow, 6).Value = item.Description;
            ws.Cell(nextRow, 7).Value = item.CountryOfManufacture;
            ws.Cell(nextRow, 8).Value = item.Supplier;
            ws.Cell(nextRow, 9).Value = item.Location;
            ws.Cell(nextRow, 10).Value = item.Remarks;
            ws.Cell(nextRow, 11).Value = item.DateAdded.ToString("yyyy-MM-dd HH:mm");

            ws.Columns().AdjustToContents();

            // Simple same-folder backup before overwriting, in case the
            // save gets interrupted partway through.
            if (File.Exists(_filePath))
                File.Copy(_filePath, _filePath + ".bak", overwrite: true);

            workbook.SaveAs(_filePath);
            workbook.Dispose();
        }
    }
}
