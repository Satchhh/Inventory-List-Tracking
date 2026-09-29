using System;

namespace InventoryApp.Models
{
    /// <summary>
    /// One item in the inventory. Add new label fields here as the
    /// project scope grows, then add the matching column in
    /// ExcelStore.Headers and ExcelStore.Append.
    /// </summary>
    public class InventoryItem
    {
        public string SerialNumber { get; set; } = "";

        // True when the item has no serial number on it (ticked in the UI).
        // When true, the serial-number duplicate check is skipped for this row.
        public bool NoSerialNumber { get; set; }

        public string ArticleNumber { get; set; } = "";

        // The short name printed on the item itself, e.g. "CP 443-1".
        // Different from Category, which is our own general grouping.
        public string TypeModel { get; set; } = "";

        public string Category { get; set; } = "";

        public string Description { get; set; } = "";

        public string CountryOfManufacture { get; set; } = "";

        public string Supplier { get; set; } = "";

        public string Location { get; set; } = "";

        public string Remarks { get; set; } = "";

        public DateTime DateAdded { get; set; } = DateTime.Now;

        /// <summary>
        /// Article number with spaces, hyphens and the "1P" scan prefix
        /// removed, and upper-cased, so "1P6ES7 407-0DA01-0AA0" and
        /// "6ES74070DA010AA0" compare as the same article.
        /// </summary>
        public string NormalizedArticleNumber => Normalize.ArticleNumber(ArticleNumber);

        /// <summary>
        /// Serial number trimmed and upper-cased for duplicate comparison.
        /// Does not strip any prefix, since we don't have solid evidence
        /// yet for which brands' serials actually carry a scanner prefix.
        /// </summary>
        public string NormalizedSerialNumber => Normalize.SerialNumber(SerialNumber);
    }

    /// <summary>
    /// Text-cleanup helpers used both when saving a new row and when
    /// comparing it against what is already in the sheet.
    /// </summary>
    public static class Normalize
    {
        public static string ArticleNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var v = value.Trim().ToUpperInvariant();
            if (v.StartsWith("1P")) v = v[2..];
            return v.Replace(" ", "").Replace("-", "");
        }

        public static string SerialNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            return value.Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Checks whether a scanned code is a well-formed EAN-13 barcode
        /// (13 digits, valid check digit) rather than a serial number.
        /// See ExcelStore for where this gets used in the UI later.
        /// </summary>
        public static bool IsValidEan13(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length != 13) return false;
            foreach (var c in code)
                if (c < '0' || c > '9') return false;

            int sum = 0;
            for (int i = 0; i < 12; i++)
                sum += (code[i] - '0') * (i % 2 == 0 ? 1 : 3);

            int checkDigit = (10 - sum % 10) % 10;
            return checkDigit == code[12] - '0';
        }
    }
}
