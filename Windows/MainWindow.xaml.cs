using DcimIngester.Controls;
using DcimIngester.Ingesting;
using System;
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
        /// The number of ingests that are currently being dealt with.
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
                NativeMethods.SHChangeNotifyEntry entry = new();
                entry.fRecursive = false;

                if (NativeMethods.SHGetKnownFolderIDList(NativeMethods.FOLDERID_DESKTOP, 0, IntPtr.Zero, out entry.pIdl) == 0)
                {
                    // TODO: Should be SHCNRF according to docs but for some reason none of those values work
                    // These values are from examples and I have no idea what they mean or do in this scenario
                    int sources = NativeMethods.SHCNF_TYPE | NativeMethods.SHCNF_IDLIST;

                    int events = NativeMethods.SHCNE_DRIVEADD | NativeMethods.SHCNE_DRIVEREMOVED |
                        NativeMethods.SHCNE_MEDIAINSERTED | NativeMethods.SHCNE_MEDIAREMOVED;

                    // Register for notifications that indicate when volumes have been added or removed
                    notifyId = NativeMethods.SHChangeNotifyRegister(windowHandle, sources, events, MESSAGE_ID, 1, ref entry);

                    if (notifyId != 0)
                    {
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
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle).RemoveHook(WndProc);
            NativeMethods.SHChangeNotifyDeregister(notifyId);
        }

        /// <summary>
        /// Invoked when the window receives a message. Reacts to messages that indicate the volumes on the system have
        /// changed.
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
                        {
                            // Invoke events in new thread to allow WndProc to return quickly
                            // TODO: Don't like this, probably a better way. Getting multiple of same volume added
                            new Thread(() => { OnVolumeAdded(path.ToString()[0]); }).Start();
                        }
                        else if (@event == NativeMethods.SHCNE_DRIVEREMOVED ||
                            @event == NativeMethods.SHCNE_MEDIAREMOVED)
                        {
                            // Invoke events in new thread to allow WndProc to return quickly
                            new Thread(() => { OnVolumeRemoved(path.ToString()[0]); }).Start();
                        }
                    }
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        private async void OnVolumeAdded(char volumeLetter)
        {
            // Don't want settings to change in the middle of an ingest
            if (((App)Application.Current).IsSettingsOpen)
                return;

            Interlocked.Increment(ref _IngestsInWork);

            await Application.Current.Dispatcher.Invoke(async () =>
            {
                IngestItem? item = StackPanel1.Children.OfType<IngestItem>()
                    .SingleOrDefault(i => i.VolumeLetter == volumeLetter);

                if (item != null)
                    RemoveItem(item);

                try
                {
                    IngestWork work = new(volumeLetter);

                    if (await work.DiscoverFilesAsync())
                        AddItem(work);
                    else Interlocked.Decrement(ref _IngestsInWork);
                }
                catch
                {
                    Interlocked.Decrement(ref _IngestsInWork);
                }
            });
        }

        private void OnVolumeRemoved(char volumeLetter)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IngestItem? item = StackPanel1.Children.OfType<IngestItem>().SingleOrDefault(
                    i => i.VolumeLetter == volumeLetter && i.Status == IngestTaskStatus.Ready);

                if (item != null)
                    RemoveItem(item);
            });
        }

        /// <summary>
        /// Creates an <see cref="IngestItem"/> from an <see cref="IngestWork"/>, displays it, and shows the window if
        /// necessary.
        /// </summary>
        /// <param name="work">The work to initialise the item with.</param>
        private void AddItem(IngestWork work)
        {
            IngestItem item = new(work);
            if (StackPanel1.Children.Count > 0)
                item.Margin = new Thickness(0, 20, 0, 0);
            item.Dismissed += IngestItem_Dismissed;

            StackPanel1.Children.Add(item);

            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;
            Show();
        }

        /// <summary>
        /// Removes an <see cref="IngestItem"/>, hides the window if necessary and decrements
        /// <see cref="_IngestsInWork"/>.
        /// </summary>
        /// <param name="item">The item to remove.</param>
        private void RemoveItem(IngestItem item)
        {
            Interlocked.Decrement(ref _IngestsInWork);
            StackPanel1.Children.Remove(item);

            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;

            if (StackPanel1.Children.Count == 0)
                Hide();
        }

        private void IngestItem_Dismissed(object? sender, EventArgs e)
        {
            RemoveItem((sender as IngestItem)!);
        }
    }
}
