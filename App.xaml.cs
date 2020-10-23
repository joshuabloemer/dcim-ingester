using DCIMIngester.Windows;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace DCIMIngester
{
    public partial class App : Application
    {
        private NotifyIcon TrayIcon = new NotifyIcon();
        private MainWindow TaskWindow = new MainWindow();

        public bool IsSettingsOpen { get; private set; }

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            TrayIcon.Text = "DCIM Ingester";
            using (Stream iconStream = GetResourceStream(new Uri(
                "pack://application:,,,/DCIMIngester;component/Resources/Icon.ico")).Stream)
            { TrayIcon.Icon = new Icon(iconStream); }

            ContextMenu trayIconMenu = new ContextMenu();
            trayIconMenu.MenuItems.Add(new MenuItem("Settings", MenuItemSettings_Click));
            trayIconMenu.MenuItems.Add(new MenuItem("-"));
            trayIconMenu.MenuItems.Add(new MenuItem("About", MenuItemAbout_Click));
            trayIconMenu.MenuItems.Add(new MenuItem("Exit", MenuItemExit_Click));

            TrayIcon.ContextMenu = trayIconMenu;
            TrayIcon.Visible = true;

            // Need to show the window to get the Loaded method to run
            TaskWindow.Show();
            //TaskWindow.Hide();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            TrayIcon.Visible = false;
        }

        private void MenuItemSettings_Click(object sender, EventArgs e)
        {
            //if (TaskWindow.Tasks.Count > 0)
            //{
            //    MessageBox.Show("Dismiss all tasks before opening settings.", "DCIM Ingester",
            //        MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK,
            //        MessageBoxOptions.DefaultDesktopOnly);
            //}
            //else
            //{
            //    IsSettingsOpen = true;
            //    Settings settingsWindow = new Settings();
            //    settingsWindow.Closed += delegate { IsSettingsOpen = false; };
            //    settingsWindow.Show();
            //}
        }
        private void MenuItemAbout_Click(object sender, EventArgs e)
        {
            FileVersionInfo versionInfo =
                FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);

            MessageBox.Show(string.Format("{0} V{1}. Created by {2}.",
                versionInfo.ProductName, versionInfo.FileVersion, versionInfo.CompanyName
                ), "DCIM Ingester", MessageBoxButton.OK, MessageBoxImage.Information,
                MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }
        private void MenuItemExit_Click(object sender, EventArgs e)
        {
            //if (TaskWindow.Tasks.Count > 0)
            //{
            //    MessageBox.Show("Dismiss all tasks before exiting.", "DCIM Ingester",
            //        MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK,
            //        MessageBoxOptions.DefaultDesktopOnly);
            //}
            //else Current.Shutdown();
        }
    }
}
