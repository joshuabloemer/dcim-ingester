using System.IO;
using System.Windows;
using System.Windows.Controls;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace DCIMIngester.Windows
{
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxEndpoint.Text = Properties.Settings.Default.Endpoint;
            TextBoxEndpoint.TextChanged += TextBoxEndpoint_TextChanged;
            ComboBoxSubfolders.SelectedIndex = Properties.Settings.Default.Subfolders;
            ComboBoxSubfolders.SelectionChanged += ComboBoxSubfolders_SelectionChanged;
        }

        private void TextBoxEndpoint_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }
        private void ButtonSelectEndpoint_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TextBoxEndpoint.Text = folderBrowser.SelectedPath;
        }
        private void ComboBoxSubfolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateFields();
        }

        private void ValidateFields()
        {
            ButtonSave.IsEnabled = TextBoxEndpoint.Text.Length > 0 ? true : false;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            // Try creating the destination directory
            try { Directory.CreateDirectory(TextBoxEndpoint.Text); }
            catch
            {
                MessageBox.Show("Unable to create destination folder", "DCIM Ingester",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Properties.Settings.Default.Endpoint = TextBoxEndpoint.Text.Replace("/", "\\").Trim()
                .TrimEnd('\\') + "\\"; // May end in multiple slashes but must end in one
            Properties.Settings.Default.Subfolders = ComboBoxSubfolders.SelectedIndex;
            Properties.Settings.Default.Save();
            Close();
        }
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
