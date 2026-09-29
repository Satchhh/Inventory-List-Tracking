using System.Windows;
using InventoryApp.Models;

namespace InventoryApp.Views
{
    public partial class DuplicateWarningWindow : Window
    {
        /// <summary>True if the user chose "Save Anyway".</summary>
        public bool SaveAnyway { get; private set; }

        public DuplicateWarningWindow(InventoryItem existingMatch)
        {
            InitializeComponent();

            MessageText.Text =
                $"Serial number \"{existingMatch.SerialNumber}\" is already in the list:";
            ExistingArticleText.Text = $"Article number: {existingMatch.ArticleNumber}";
            ExistingTypeText.Text = $"Type / model: {existingMatch.TypeModel}";
            ExistingLocationText.Text = $"Location: {existingMatch.Location}";
        }

        private void SaveAnyway_Click(object sender, RoutedEventArgs e)
        {
            SaveAnyway = true;
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            SaveAnyway = false;
            DialogResult = false;
        }
    }
}
