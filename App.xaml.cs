using DCIMIngester.Windows;
using System;
using System.Drawing;
using System.IO;
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

            MenuItem menuItemSettings = new MenuItem();
            menuItemSettings.Text = "Settings";
            menuItemSettings.Click += MenuItemSettings_Click;
            MenuItem menuItemExit = new MenuItem();
            menuItemExit.Text = "Exit";
            menuItemExit.Click += MenuItemExit_Click;

            ContextMenu trayIconMenu = new ContextMenu();
            trayIconMenu.MenuItems.Add(menuItemSettings);
            trayIconMenu.MenuItems.Add(menuItemExit);

            TrayIcon.ContextMenu = trayIconMenu;
            TrayIcon.Visible = true;

            // Need to show the window to get the Loaded method to run
            TaskWindow.Show();
            TaskWindow.Hide();
        }
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            TrayIcon.Visible = false;
        }

        private void MenuItemSettings_Click(object sender, EventArgs e)
        {
            if (TaskWindow.Tasks.Count > 0)
            {
                MessageBox.Show("Dismiss all tasks before opening settings.",
                    "DCIM Ingester");
            }
            else
            {
                IsSettingsOpen = true;
                Settings settingsWindow = new Settings();
                settingsWindow.Closed += delegate { IsSettingsOpen = false; };
                settingsWindow.Show();
            }
        }
        private void MenuItemExit_Click(object sender, EventArgs e)
        {
            if (TaskWindow.Tasks.Count > 0)
                MessageBox.Show("Dismiss all tasks before exiting.", "DCIM Ingester");
            else Current.Shutdown();
        }
    }
}
