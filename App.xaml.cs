using System.Windows;

namespace DCIMIngester
{
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            Windows.MainWindow mainWindow = new Windows.MainWindow();
            mainWindow.Show();
            mainWindow.Hide();
        }
    }
}
