using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using InventoryApp.Models;
using InventoryApp.Services;
using InventoryApp.Views;

namespace InventoryApp
{
    public partial class MainWindow : Window
    {
        // TODO: point this at the real shared Excel file once you know
        // where it will live (a synced folder, a network path, etc.)
        private readonly ExcelStore _store = new(
            System.IO.Path.Combine(Environment.CurrentDirectory, "Inventory.xlsx"));

        private ObservableCollection<InventoryItem> _recent = new();

        public MainWindow()
        {
            InitializeComponent();
            RecentEntriesList.ItemsSource = _recent;
            LoadRecent();
            SerialBox.Focus();
        }

        private void LoadRecent()
        {
            var all = _store.LoadAll();
            _recent.Clear();
            foreach (var item in all.AsEnumerable().Reverse().Take(10))
                _recent.Add(item);
        }

        private void NoSerialCheck_Changed(object sender, RoutedEventArgs e)
        {
            SerialBox.IsEnabled = NoSerialCheck.IsChecked != true;
            if (NoSerialCheck.IsChecked == true)
                SerialBox.Clear();
        }

        private InventoryItem BuildItemFromForm()
        {
            return new InventoryItem
            {
                SerialNumber = SerialBox.Text.Trim(),
                NoSerialNumber = NoSerialCheck.IsChecked == true,
                ArticleNumber = ArticleBox.Text.Trim(),
                TypeModel = TypeModelCombo.Text.Trim(),
                Category = (CategoryCombo.SelectedItem as ComboBoxItem)?.Content?.ToString()
                           ?? CategoryCombo.Text.Trim(),
                Description = DescriptionBox.Text.Trim(),
                CountryOfManufacture = CountryCombo.Text.Trim(),
                Supplier = SupplierBox.Text.Trim(),
                Location = LocationCombo.Text.Trim(),
                Remarks = RemarksBox.Text.Trim(),
            };
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var item = BuildItemFromForm();

            // Required fields. Serial is only required when "no serial" isn't ticked.
            if (string.IsNullOrWhiteSpace(item.ArticleNumber) ||
                (string.IsNullOrWhiteSpace(item.SerialNumber) && !item.NoSerialNumber))
            {
                MessageBox.Show(
                    "Please fill in the Article Number, and either the Serial Number " +
                    "or tick \"No serial number\".",
                    "Missing information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var existing = _store.LoadAll();
            var dupCheck = _store.CheckForDuplicates(item, existing);

            if (dupCheck.IsDuplicateSerial && dupCheck.ExistingSerialMatch != null)
            {
                var dialog = new DuplicateWarningWindow(dupCheck.ExistingSerialMatch) { Owner = this };
                var confirmed = dialog.ShowDialog();
                if (confirmed != true || !dialog.SaveAnyway)
                    return; // user cancelled - stay on the form so they can check
            }

            try
            {
                _store.Append(item);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could not save", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            LoadRecent();
            ClearForm();
            ShowSavedMessage();
            SerialBox.Focus();
        }

        private void ShowSavedMessage()
        {
            StatusText.Text = "✓ Saved";
            StatusText.Visibility = Visibility.Visible;
        }

        private void ClearForm()
        {
            SerialBox.Clear();
            NoSerialCheck.IsChecked = false;
            ArticleBox.Clear();
            TypeModelCombo.Text = "";
            CategoryCombo.SelectedIndex = -1;
            DescriptionBox.Clear();
            CountryCombo.Text = "";
            SupplierBox.Clear();
            // Location is deliberately NOT cleared - consecutive items are
            // often entered from the same cabinet/shelf, so keeping it
            // saves a re-pick on every single row.
            RemarksBox.Clear();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            ClearForm();
            StatusText.Visibility = Visibility.Collapsed;
            SerialBox.Focus();
        }

        // --- Copy buttons -------------------------------------------------
        // These exist so the person can grab a value with one click instead
        // of dragging the mouse to select text before copying it.

        private void CopySerial_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SerialBox.Text))
                Clipboard.SetText(SerialBox.Text);
        }

        private void CopyArticle_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(ArticleBox.Text))
                Clipboard.SetText(ArticleBox.Text);
        }

        private void CopyRow_Click(object sender, RoutedEventArgs e)
        {
            var item = BuildItemFromForm();
            // Tab-separated so pasting into Excel's first cell of a row
            // fills every column across in one paste.
            var row = string.Join("\t", new[]
            {
                item.NoSerialNumber ? "UNKNOWN" : item.SerialNumber,
                item.ArticleNumber,
                item.TypeModel,
                item.Category,
                item.Description,
                item.CountryOfManufacture,
                item.Supplier,
                item.Location,
                item.Remarks,
            });
            Clipboard.SetText(row);

            StatusText.Text = "✓ Row copied - paste into a cell in Excel";
            StatusText.Visibility = Visibility.Visible;
        }
    }
}
