using DcimIngester.Controls;
using DcimIngester.Ingesting;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
        private BlockingCollection<(char, bool)> volumeNotifQueue = new();

        /// <summary>
        /// Thread for handling received volume change notifications.
        /// </summary>
        private Thread? volumeNotifThread = null;

        /// <summary>
        /// Used to cancel the blocking TryTake call used to wait for items in <see cref="volumeNotifQueue"/>.
        /// </summary>
        private readonly CancellationTokenSource queueTakeCancel = new();

        /// <summary>
        /// The number of ingests currently being dealt with. This increments when a volume change notification is
        /// received.
        /// </summary>
        public int IngestsInWork => _IngestsInWork;

        private int _IngestsInWork = 0;


        /// <summary>
        /// Initialises a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;

            bool shutdown = true;

            // TODO: This works, but should be GetWindowLongPtr intead
            int extendedStyle = NativeMethods.GetWindowLong(windowHandle, NativeMethods.GWL_EXSTYLE);
            extendedStyle |= NativeMethods.WS_EX_TOOLWINDOW;

            // Hide window from Windows task switcher by making it a tool window
            // TODO: This works, but should be SetWindowLongPtr intead
            if (NativeMethods.SetWindowLong(windowHandle, NativeMethods.GWL_EXSTYLE, extendedStyle) != 0)
            {
                NativeMethods.SHChangeNotifyEntry entry = new()
                {
                    fRecursive = false
                };

                if (NativeMethods.SHGetKnownFolderIDList(NativeMethods.FOLDERID_DESKTOP, 0, IntPtr.Zero, out entry.pIdl) == 0)
                {
                    // TODO: Should be SHCNRF according to docs but for some reason none of those values work
                    // These values are from examples and I have no idea what they mean or do in this scenario
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
                        shutdown = false;
                    }
                }
            }

            if (shutdown)
                ((App)Application.Current).Shutdown();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (notifyId > 0)
            {
                HwndSource.FromHwnd(new WindowInteropHelper(this).Handle).RemoveHook(WndProc);
                queueTakeCancel.Cancel();
                NativeMethods.SHChangeNotifyDeregister(notifyId);
            }
        }

        /// <summary>
        /// Invoked when the window receives a message. Reacts to messages that indicate that the volumes on the system
        /// have changed.
        /// </summary>
        private IntPtr WndProc(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == MESSAGE_ID)
            {
                Interlocked.Increment(ref _IngestsInWork);

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
                    else Interlocked.Decrement(ref _IngestsInWork);
                }
                else Interlocked.Decrement(ref _IngestsInWork);
            }

            handled = false;
            return IntPtr.Zero;
        }

        /// <summary>
        /// Handles queued volume addition and removal notifications. Runs in a separate thread.
        /// </summary>
        private void VolumeNotifThread()
        {
            // char is volume letter, bool is true if volume was removed
            (char, bool) notification;

            while (volumeNotifQueue.TryTake(out notification, Timeout.Infinite, queueTakeCancel.Token))
            {
                if (notification.Item2)
                    OnVolumeRemoved(notification.Item1);
                else OnVolumeAdded(notification.Item1);
            }
        }

        /// <summary>
        /// Invoked when a volume addition notification is handled.
        /// </summary>
        /// <param name="volumeLetter">The letter of the volume.</param>
        private void OnVolumeAdded(char volumeLetter)
        {
            // Don't want settings to change in the middle of an ingest
            if (!((App)Application.Current).IsSettingsOpen)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IngestItem? item = StackPanel1.Children.OfType<IngestItem>()
                        .SingleOrDefault(i => i.VolumeLetter == volumeLetter);

                    // Not cancelling first because it should have failed if in progress
                    if (item != null)
                        RemoveItem(item);
                });

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
                    else Interlocked.Decrement(ref _IngestsInWork);
                }
                catch
                {
                    Interlocked.Decrement(ref _IngestsInWork);
                }
            }
            else Interlocked.Decrement(ref _IngestsInWork);
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
            Interlocked.Decrement(ref _IngestsInWork);

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
    }
}
