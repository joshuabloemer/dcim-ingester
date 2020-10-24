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
            TextBoxDestination.Text = Properties.Settings.Default.Destination;
            ComboBoxSubfolders.SelectedIndex = Properties.Settings.Default.Subfolders;
        }

        private void TextBoxDestination_TextChanged(object sender, TextChangedEventArgs e)
        {
            ValidateFields();
        }
        private void ButtonBrowseDestination_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();

            if (folderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TextBoxDestination.Text = folderDialog.SelectedPath;
        }
        private void ComboBoxSubfolders_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ValidateFields();
        }

        private void ValidateFields()
        {
            if (TextBoxDestination.Text != Properties.Settings.Default.Destination ||
                ComboBoxSubfolders.SelectedIndex != Properties.Settings.Default.Subfolders)
            {
                if (TextBoxDestination.Text.Length > 0)
                {
                    ButtonSave.IsEnabled = true;
                    return;
                }
            }

            ButtonSave.IsEnabled = false;
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Destination = TextBoxDestination.Text;
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
