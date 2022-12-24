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
using System.Threading;

namespace DcimIngester
{
    public partial class App : Application
    {
        private TaskbarIcon? taskbarIcon = null;
        private MainWindow? mainWindow = null;

        private bool isSettingsOpen = false;
        private static bool AlreadyProcessedOnThisInstance;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            MakeSingleInstance(Utilities.AssemblyGuid);

            mainWindow = new MainWindow();

            taskbarIcon = new()
            {
                ToolTipText = "DCIM Ingester",
                ContextMenu = (ContextMenu)FindResource("TaskbarIconContextMenu")
            };

            using (Stream stream = GetResourceStream(new Uri(
                "pack://application:,,,/DcimIngester;component/Icon.ico")).Stream)
            {
                taskbarIcon.Icon = new Icon(stream);
            }

            taskbarIcon.Visibility = Visibility.Visible;

            if (DcimIngester.Properties.Settings.Default.Destination.Length == 0
                || DcimIngester.Properties.Settings.Default.Rules.Length == 0)
                MenuItemSettings_Click(this, new RoutedEventArgs());

            mainWindow = new MainWindow();

            // Need to show the window to get the Loaded event to trigger
            mainWindow.Show();
            mainWindow.Hide();
        }

        private void MakeSingleInstance(string appName, bool uniquePerUser = true)
        {
            if (AlreadyProcessedOnThisInstance)
            {
                return;
            }
            AlreadyProcessedOnThisInstance = true;

            Application app = Application.Current;

            string eventName = uniquePerUser
                ? $"{appName}-{Environment.MachineName}-{Environment.UserDomainName}-{Environment.UserName}"
                : $"{appName}-{Environment.MachineName}";

            bool isSecondaryInstance = true;

            EventWaitHandle eventWaitHandle = null;
            try
            {
                eventWaitHandle = EventWaitHandle.OpenExisting(eventName);
            }
            catch
            {
                // This code only runs on the first instance.
                isSecondaryInstance = false;
            }

            if (isSecondaryInstance)
            {
                ActivateFirstInstanceWindow(eventWaitHandle);

                // Let's produce a non-interceptable exit (2009 year approach).
                Environment.Exit(0);
            }

            RegisterFirstInstanceWindowActivation(app, eventName);
        }

        private void ActivateFirstInstanceWindow(EventWaitHandle eventWaitHandle)
        {
            // Let's notify the first instance to activate its main window.
            _ = eventWaitHandle.Set();
        }

        private void RegisterFirstInstanceWindowActivation(Application app, string eventName)
        {
            EventWaitHandle eventWaitHandle = new EventWaitHandle(
                false,
                EventResetMode.AutoReset,
                eventName);

            _ = ThreadPool.RegisterWaitForSingleObject(eventWaitHandle, WaitOrTimerCallback, app, Timeout.Infinite, false);

            eventWaitHandle.Close();
        }

        private void WaitOrTimerCallback(object state, bool timedOut)
        {
            Application app = (Application)state;
            _ = app.Dispatcher.BeginInvoke(new Action(() =>
            {
                MenuItemSettings_Click(this, new RoutedEventArgs());
            }));
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
                MessageBox.Show(versionString, "DCIM Ingester", MessageBoxButton.OK, MessageBoxImage.None,
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
