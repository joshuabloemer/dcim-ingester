using System.Windows;
using System.Windows.Forms;

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
        }

        private void ButtonSelectEndpoint_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderBrowser = new FolderBrowserDialog();
            if (folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                TextBoxEndpoint.Text = folderBrowser.SelectedPath;
                ButtonSave.IsEnabled = true;
            }
        }

        private void ButtonSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.Endpoint = TextBoxEndpoint.Text;
            Properties.Settings.Default.Save();
            Close();
        }
        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
