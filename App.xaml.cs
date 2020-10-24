using DcimIngester.Windows;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using ContextMenu = System.Windows.Forms.ContextMenu;
using MenuItem = System.Windows.Forms.MenuItem;
using NotifyIcon = System.Windows.Forms.NotifyIcon;

namespace DcimIngester
{
    public partial class App : Application
    {
        private readonly NotifyIcon notifyIcon = new NotifyIcon();
        private readonly MainWindow mainWindow = new MainWindow();

        public bool IsSettingsOpen { get; private set; } = false;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            ContextMenu menu = new ContextMenu();
            menu.MenuItems.Add(new MenuItem("Settings", MenuItemSettings_Click));
            menu.MenuItems.Add(new MenuItem("-"));
            menu.MenuItems.Add(new MenuItem("About", MenuItemAbout_Click));
            menu.MenuItems.Add(new MenuItem("Exit", MenuItemExit_Click));
            notifyIcon.ContextMenu = menu;

            using (Stream iconStream = GetResourceStream(new Uri(
                "pack://application:,,,/DcimIngester;component/Resources/Icon.ico")).Stream)
            {
                notifyIcon.Icon = new Icon(iconStream);
            }

            notifyIcon.Text = "DCIM Ingester";
            notifyIcon.Visible = true;

            // We need to show the window to get the Loaded method to run
            mainWindow.Show();
            mainWindow.Hide();
        }
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            notifyIcon.Visible = false;
        }

        private void MenuItemSettings_Click(object sender, EventArgs e)
        {
            if (mainWindow.TaskCount > 0)
            {
                MessageBox.Show("Dismiss all tasks before opening settings.", "DCIM Ingester",
                    MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK,
                    MessageBoxOptions.DefaultDesktopOnly);
            }
            else
            {
                IsSettingsOpen = true;
                Settings settingsWindow = new Settings();
                settingsWindow.Closed += delegate { IsSettingsOpen = false; };
                settingsWindow.Show();
            }
        }
        private void MenuItemAbout_Click(object sender, EventArgs e)
        {
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly().Location);
            string versionString = string.Format("{0} V{1}. Created by {2}.",
                versionInfo.ProductName, versionInfo.FileVersion, versionInfo.CompanyName);

            MessageBox.Show(versionString, "DCIM Ingester", MessageBoxButton.OK, MessageBoxImage.Information,
                MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }
        private void MenuItemExit_Click(object sender, EventArgs e)
        {
            if (mainWindow.TaskCount > 0)
            {
                MessageBox.Show("Dismiss all tasks before exiting.", "DCIM Ingester", MessageBoxButton.OK,
                    MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
            }
            else Current.Shutdown();
        }
    }
}
