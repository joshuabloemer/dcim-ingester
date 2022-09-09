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
            TextBoxDestDir.Text = Properties.Settings.Default.DestDirectory;
            ComboBoxDestStruc.SelectedIndex = Properties.Settings.Default.DestStructure;
        }

        private void TextBoxDestDir_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }

        private void ButtonBrowseDest_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new();

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TextBoxDestDir.Text = folderDialog.SelectedPath;
        }

        private void ComboBoxDestStruc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateFields();
        }

        private void ValidateFields()
        {
            if ((TextBoxDestDir.Text.Length > 0 &&
                TextBoxDestDir.Text != Properties.Settings.Default.DestDirectory) ||
                ComboBoxDestStruc.SelectedIndex != Properties.Settings.Default.DestStructure)
            {
                ButtonSave.IsEnabled = true;
            }
            else ButtonSave.IsEnabled = false;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.DestDirectory = TextBoxDestDir.Text;
            Properties.Settings.Default.DestStructure = ComboBoxDestStruc.SelectedIndex;
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
