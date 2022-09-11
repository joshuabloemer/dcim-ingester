using DcimIngester.Windows;
using Hardcodet.Wpf.TaskbarNotification;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace DcimIngester
{
    public partial class App : Application
    {
        private TaskbarIcon? taskbarIcon = null;
        private MainWindow? mainWindow = null;

        private bool isSettingsOpen = false;

        private void Application_Startup(object sender, StartupEventArgs e)
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

            if (DcimIngester.Properties.Settings.Default.DestDirectory.Length == 0)
                MenuItemSettings_Click(this, new RoutedEventArgs());

            mainWindow = new MainWindow();

            // Need to show the window to get the Loaded event to trigger
            mainWindow.Show();
            mainWindow.Hide();
        }

        private void MenuItemSettings_Click(object sender, RoutedEventArgs e)
        {
            if (!isSettingsOpen)
            {
                isSettingsOpen = true;

                // Disable Settings item in context menu
                ((MenuItem)taskbarIcon!.ContextMenu.Items[0]).IsEnabled = false;

                new Settings().Show();
                ((MenuItem)taskbarIcon!.ContextMenu.Items[0]).IsEnabled = true;
                isSettingsOpen = false;
            }
        }

        private void MenuItemAbout_Click(object sender, RoutedEventArgs e)
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location);

            string versionString = string.Format("{0} V{1}. Created by {2}.",
                versionInfo.ProductName, versionInfo.FileVersion, versionInfo.CompanyName);

            Task.Run(() =>
            {
                MessageBox.Show(versionString, "DCIM Ingester", MessageBoxButton.OK, MessageBoxImage.Information,
                    MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            });
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
                Task.Run(() =>
                {
                    MessageBox.Show("Wait for ingests to finish before exiting.", "DCIM Ingester",
                        MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK,
                        MessageBoxOptions.DefaultDesktopOnly);
                });
            }
        }
    }
}
