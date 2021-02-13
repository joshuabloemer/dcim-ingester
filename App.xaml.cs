using DcimIngester.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace DcimIngester
{
    public partial class App : Application
    {
        private readonly TaskbarIcon taskbarIcon = new TaskbarIcon();
        private readonly MainWindow mainWindow = new MainWindow();

        public bool IsSettingsOpen { get; private set; } = false;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            taskbarIcon.ToolTip = "DCIM Ingester";
            taskbarIcon.ContextMenu = (ContextMenu)FindResource("TaskbarIconContextMenu");

            using (Stream stream = GetResourceStream(new Uri(
                "pack://application:,,,/DcimIngester;component/Icon.ico")).Stream)
            {
                taskbarIcon.Icon = new Icon(stream);
            }

            taskbarIcon.Visibility = Visibility.Visible;

            // We need to show the window to get the Loaded method to run
            mainWindow.Show();
            mainWindow.Hide();
        }
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            taskbarIcon.Visibility = Visibility.Collapsed;
        }

        private void MenuItemSettings_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow.WorkCount > 0)
            {
                MessageBox.Show("Dismiss all ingests before opening Settings.", "DCIM Ingester",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly);
            }
            else
            {
                IsSettingsOpen = true;
                Settings settings = new Settings();
                settings.Closed += delegate { IsSettingsOpen = false; };
                settings.Show();
            }
        }
        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            FileVersionInfo versionInfo =
                FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location);

            string versionString = string.Format("{0} V{1}. Created by {2}.",
                versionInfo.ProductName, versionInfo.FileVersion, versionInfo.CompanyName);

            MessageBox.Show(versionString, "DCIM Ingester", MessageBoxButton.OK,
                MessageBoxImage.Information, MessageBoxResult.OK,
                MessageBoxOptions.DefaultDesktopOnly);
        }
        private void MenuItemExit_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow.WorkCount > 0)
            {
                MessageBox.Show("Dismiss all ingests before exiting.", "DCIM Ingester",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly);
            }
            else Current.Shutdown();
        }
    }
}
