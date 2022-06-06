using DcimIngester.Controls;
using DcimIngester.Ingesting;
using DcimIngester.VolumeWatching;
using System;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace DcimIngester.Windows
{
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Watches for volumes being mounted to or unmounted from the system.
        /// </summary>
        private VolumeWatcher? volumeWatcher = null;

        /// <summary>
        /// The number of ingests that are currently being dealt with.
        /// </summary>
        private int _IngestsInWork = 0;

        /// <summary>
        /// The number of ingests that are currently being dealt with.
        /// </summary>
        public int IngestsInWork => _IngestsInWork;

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

            // Hide window from the Windows task switcher by making it a tool window
            int extendedStyle = NativeMethods.GetWindowLong(windowHandle, NativeMethods.GWL_EXSTYLE) |
                NativeMethods.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLong(windowHandle, NativeMethods.GWL_EXSTYLE, extendedStyle);

            volumeWatcher = new VolumeWatcher();
            volumeWatcher.VolumeAdded += VolumeWatcher_VolumeAdded;
            volumeWatcher.VolumeRemoved += VolumeWatcher_VolumeRemoved;
            volumeWatcher.StartWatching();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            volumeWatcher!.StopWatching();
        }
        
        private async void VolumeWatcher_VolumeAdded(object? sender, VolumeChangedEventArgs e)
        {
            // Don't want settings to change in the middle of an ingest
            if (((App)Application.Current).IsSettingsOpen)
                return;

            Interlocked.Increment(ref _IngestsInWork);

            await Application.Current.Dispatcher.Invoke(async () =>
            {
                IngestItem? item = StackPanel1.Children.OfType<IngestItem>()
                    .SingleOrDefault(i => i.VolumeLetter == e.VolumeLetter);

                if (item != null)
                    RemoveItem(item);

                try
                {
                    IngestWork work = new IngestWork(e.VolumeLetter);

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

        private void VolumeWatcher_VolumeRemoved(object? sender, VolumeChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                IngestItem? item = StackPanel1.Children.OfType<IngestItem>().SingleOrDefault(
                    i => i.VolumeLetter == e.VolumeLetter && i.Status == IngestTaskStatus.Ready);

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
            IngestItem item = new IngestItem(work);
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
