using DcimIngester.Controls;
using DcimIngester.Ingesting;
using Microsoft.Win32;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;

namespace DcimIngester.Windows
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// The identifier returned from registering for volume change notifications.
        /// </summary>
        private uint notifyId = 0;

        /// <summary>
        /// The message ID that will be passed to WndProc when the volumes on the system have changed.
        /// </summary>
        private const uint MESSAGE_ID = 0x0401;

        /// <summary>
        /// Stores received volume change notifications. Items contain volume letter and <see langword="true"/> if 
        /// volume was removed.
        /// </summary>
        private readonly BlockingCollection<(char, bool)> volumeNotifQueue = new();

        /// <summary>
        /// Thread for handling received volume change notifications.
        /// </summary>
        private Thread? volumeNotifThread = null;

        /// <summary>
        /// Used to cancel the blocking TryTake call used to wait for items in <see cref="volumeNotifQueue"/>.
        /// </summary>
        private readonly CancellationTokenSource queueTakeCancel = new();

        /// <summary>
        /// Gets the number of <see cref="IngestItem"/>s that are currently actively ingesting.
        /// </summary>
        public int ActiveIngestCount => GetActiveIngestCount();


        /// <summary>
        /// Initialises a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            bool shutdown = true;
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;

            if (HideWindowFromAltTab(windowHandle))
            {
                NativeMethods.SHChangeNotifyEntry entry = new()
                {
                    fRecursive = false
                };

                if (NativeMethods.SHGetKnownFolderIDList(NativeMethods.FOLDERID_DESKTOP, 0, IntPtr.Zero, out entry.pIdl) == 0)
                {
                    // Should be SHCNRF according to docs but for some reason none of those values work
                    // These values are from examples and I have no idea what they mean or do here
                    int sources = NativeMethods.SHCNF_TYPE | NativeMethods.SHCNF_IDLIST;

                    int events = NativeMethods.SHCNE_DRIVEADD | NativeMethods.SHCNE_DRIVEREMOVED |
                        NativeMethods.SHCNE_MEDIAINSERTED | NativeMethods.SHCNE_MEDIAREMOVED;

                    // Register for notifications that indicate when volumes have been added or removed
                    notifyId = NativeMethods.SHChangeNotifyRegister(windowHandle, sources, events, MESSAGE_ID, 1, ref entry);

                    if (notifyId > 0)
                    {
                        volumeNotifThread = new Thread(VolumeNotifThread);
                        volumeNotifThread.Start();

                        HwndSource.FromHwnd(windowHandle).AddHook(WndProc);
                        SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
                        shutdown = false;
                    }
                }
            }

            if (shutdown)
                ((App)Application.Current).Shutdown();
        }

        private void Window_Closing(object sender, EventArgs e)
        {
            if (notifyId > 0)
            {
                HwndSource.FromHwnd(new WindowInteropHelper(this).Handle).RemoveHook(WndProc);
                queueTakeCancel.Cancel();
                NativeMethods.SHChangeNotifyDeregister(notifyId);
                SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
            }
        }

        /// <summary>
        /// Hides the window from the Alt+Tab task switcher.
        /// </summary>
        /// <param name="windowHandle">The handle of the window.</param>
        /// <returns><see langword="true"/> on success, otherwise <see langword="false"/>.</returns>
        private bool HideWindowFromAltTab(IntPtr windowHandle)
        {
            int extendedStyle = NativeMethods.GetWindowLongPtr(windowHandle, NativeMethods.GWL_EXSTYLE);

            if (extendedStyle != 0)
            {
                extendedStyle |= NativeMethods.WS_EX_TOOLWINDOW;

                if (NativeMethods.SetWindowLongPtr(windowHandle, NativeMethods.GWL_EXSTYLE, extendedStyle) != 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Invoked when the window receives a message. Reacts to messages that indicate that the volumes on the system
        /// have changed.
        /// </summary>
        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == MESSAGE_ID)
            {
                NativeMethods.SHNotifyStruct notif = (NativeMethods.SHNotifyStruct)
                    Marshal.PtrToStructure(wParam, typeof(NativeMethods.SHNotifyStruct))!;

                int @event = (int)lParam;

                if (@event == NativeMethods.SHCNE_DRIVEADD || @event == NativeMethods.SHCNE_DRIVEREMOVED ||
                    @event == NativeMethods.SHCNE_MEDIAINSERTED || @event == NativeMethods.SHCNE_MEDIAREMOVED)
                {
                    StringBuilder path = new(3);

                    // Get the path of the volume that changed (expecting "X:/")
                    if (NativeMethods.SHGetPathFromIDListW(notif.dwItem1, path) && path.Length == 3)
                    {
                        if (@event == NativeMethods.SHCNE_DRIVEADD || @event == NativeMethods.SHCNE_MEDIAINSERTED)
                            volumeNotifQueue.Add((path.ToString()[0], false));
                        else if (@event == NativeMethods.SHCNE_DRIVEREMOVED || @event == NativeMethods.SHCNE_MEDIAREMOVED)
                            volumeNotifQueue.Add((path.ToString()[0], true));
                    }
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        private void SystemEvents_DisplaySettingsChanged(object? sender, EventArgs e)
        {
            if (Visibility == Visibility.Visible)
            {
                Left = SystemParameters.WorkArea.Right - Width - 20;
                Top = SystemParameters.WorkArea.Bottom - Height - 20;

                // WorkArea does not properly account for the taskbar at the time this event is
                // fired, so need to reposition again after a short wait. Also, don't await because
                // this method should return quickly.
                Task.Delay(500).ContinueWith(t =>
                {
                    Left = SystemParameters.WorkArea.Right - Width - 20;
                    Top = SystemParameters.WorkArea.Bottom - Height - 20;
                });
            }
        }

        /// <summary>
        /// Handles queued volume addition and removal notifications. Runs in a separate thread.
        /// </summary>
        private void VolumeNotifThread()
        {
            try
            {
                // In tuple, char is volume letter, bool is true if volume was removed

                while (volumeNotifQueue.TryTake(
                    out (char, bool) notification, Timeout.Infinite, queueTakeCancel.Token))
                {
                    if (notification.Item2)
                        OnVolumeRemoved(notification.Item1);
                    else OnVolumeAdded(notification.Item1);
                }
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Invoked when a volume addition notification is handled.
        /// </summary>
        /// <param name="volumeLetter">The letter of the volume.</param>
        private void OnVolumeAdded(char volumeLetter)
        {
            if (Properties.Settings.Default.Destination.Length == 0)
                return;

            bool discover = true;

            Application.Current.Dispatcher.Invoke(() =>
            {
                IngestItem? item = StackPanel1.Children.OfType<IngestItem>()
                    .SingleOrDefault(i => i.VolumeLetter == volumeLetter);

                // Not cancelling first because it should have failed if in
                // progress. But just in case it hasn't, don't continue
                if (item != null)
                {
                    if (item.Status != IngestTaskStatus.Ingesting)
                        RemoveItem(item);
                    else discover = false;
                }
            });

            if (discover)
            {
                try
                {
                    IngestWork work = new(volumeLetter);

                    if (work.DiscoverFiles())
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            IngestItem item = new(work);
                            if (StackPanel1.Children.Count > 0)
                                item.Margin = new Thickness(0, 20, 0, 0);
                            item.Dismissed += IngestItem_Dismissed;

                            StackPanel1.Children.Add(item);

                            Left = SystemParameters.WorkArea.Right - Width - 20;
                            Top = SystemParameters.WorkArea.Bottom - Height - 20;
                            Show();
                        });
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// Invoked when a volume removal notification is handled.
        /// </summary>
        /// <param name="volumeLetter">The letter of the volume.</param>
        private void OnVolumeRemoved(char volumeLetter)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Only remove items that have not been started
                IngestItem? item = StackPanel1.Children.OfType<IngestItem>().SingleOrDefault(
                    i => i.VolumeLetter == volumeLetter && i.Status == IngestTaskStatus.Ready);

                if (item != null)
                    RemoveItem(item);
            });
        }

        /// <summary>
        /// Removes an <see cref="IngestItem"/> from the UI and hides the window if no items are left.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        private void RemoveItem(IngestItem item)
        {
            StackPanel1.Children.Remove(item);

            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;

            if (StackPanel1.Children.Count == 0)
                Hide();
        }

        private void IngestItem_Dismissed(object? sender, EventArgs e)
        {
            // Need to check because it may be possible for a volume removal notification to remove the
            // item being dismissed between the dismiss button being clicked and this code executing
            if (StackPanel1.Children.Contains((IngestItem)sender!))
                RemoveItem((IngestItem)sender!);
        }

        /// <summary>
        /// Returns the number of <see cref="IngestItem"/>s that are currently actively ingesting.
        /// </summary>
        private int GetActiveIngestCount()
        {
            return StackPanel1.Children.OfType<IngestItem>().Count(
                i => i.Status == IngestTaskStatus.Ingesting);
        }
    }
}
