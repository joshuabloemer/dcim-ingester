using System.Windows;
using System.Windows.Controls;
using FolderBrowserDialog = System.Windows.Forms.FolderBrowserDialog;

namespace DcimIngester.Windows
{
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TextBoxDestination.Text = Properties.Settings.Default.DestDirectory;
            ComboBoxSubfolders.SelectedIndex = Properties.Settings.Default.DestStructure;
        }

        private void TextBoxDestination_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }

        private void ButtonBrowseDest_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new();

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TextBoxDestination.Text = folderDialog.SelectedPath;
        }

        private void ComboBoxSubfolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateFields();
        }

        private void ValidateFields()
        {
            if ((TextBoxDestination.Text.Length > 0 &&
                TextBoxDestination.Text != Properties.Settings.Default.DestDirectory) ||
                ComboBoxSubfolders.SelectedIndex != Properties.Settings.Default.DestStructure)
            {
                ButtonSave.IsEnabled = true;
            }
            else ButtonSave.IsEnabled = false;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DestDirectory = TextBoxDestination.Text;
            Properties.Settings.Default.DestStructure = ComboBoxSubfolders.SelectedIndex;
            Properties.Settings.Default.Save();

            DialogResult = true;
            Close();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
