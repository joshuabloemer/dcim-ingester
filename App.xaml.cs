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
        private TaskbarIcon? taskbarIcon = null;
        private MainWindow? mainWindow = null;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            bool shutdown = false;

            if (DcimIngester.Properties.Settings.Default.DestDirectory.Length == 0)
            {
                if (new Settings().ShowDialog() == false)
                    shutdown = true;
            }

            if (!shutdown)
            {
                taskbarIcon = new()
                {
                    ToolTip = "DCIM Ingester",
                    ContextMenu = (ContextMenu)FindResource("TaskbarIconContextMenu")
                };

                using (Stream stream = GetResourceStream(new Uri(
                    "pack://application:,,,/DcimIngester;component/Icon.ico")).Stream)
                {
                    taskbarIcon.Icon = new Icon(stream);
                }

                taskbarIcon.Visibility = Visibility.Visible;

                mainWindow = new MainWindow();

                // Need to show the window to get the Loaded event to trigger
                mainWindow.Show();
                mainWindow.Hide();
            }
            else Shutdown();
        }

        private void MenuItemSettings_Click(object sender, RoutedEventArgs e)
        {
            new Settings().ShowDialog();
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
            if (mainWindow!.ActiveIngestCount == 0)
            {
                // Icon stays visible after shutdown (until hovered over) without this
                taskbarIcon!.Visibility = Visibility.Collapsed;

                Shutdown();
            }
            else
            {
                MessageBox.Show("Wait for ingests to finish before exiting.", "DCIM Ingester",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly);
            }
        }
    }
}
