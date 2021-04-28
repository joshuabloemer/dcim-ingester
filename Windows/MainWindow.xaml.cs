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
        private VolumeWatcher? volumeWatcher = null;

        private int workCount = 0;
        public int WorkCount => workCount;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            volumeWatcher = new VolumeWatcher(HwndSource.FromHwnd(windowHandle));

            // Hide window from the Windows task switcher by making it a tool window
            int extendedStyle = NativeMethods.GetWindowLong(windowHandle, NativeMethods.GWL_EXSTYLE) |
                NativeMethods.WS_EX_TOOLWINDOW;
            NativeMethods.SetWindowLong(windowHandle, NativeMethods.GWL_EXSTYLE, extendedStyle);

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

            Interlocked.Increment(ref workCount);

            await Application.Current.Dispatcher.Invoke(async () =>
            {
                IngestItem? item = StackPanel1.Children.OfType<IngestItem>().SingleOrDefault(
                    i => i.VolumeLetter == e.VolumeLetter);

                if (item != null)
                    RemoveItem(item);

                try
                {
                    IngestWork work = new IngestWork(e.VolumeLetter);

                    if (await work.DiscoverFilesAsync())
                        AddItem(work);
                    else Interlocked.Decrement(ref workCount);
                }
                catch
                {
                    Interlocked.Decrement(ref workCount);
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

        private void AddItem(IngestWork work)
        {
            IngestItem item = new IngestItem(work);
            if (StackPanel1.Children.Count > 0)
                item.Margin = new Thickness(0, 20, 0, 0);
            item.Dismissed += Item_Dismissed;

            StackPanel1.Children.Add(item);

            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;
            Show();
        }
        private void RemoveItem(IngestItem item)
        {
            Interlocked.Decrement(ref workCount);
            StackPanel1.Children.Remove(item);

            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;

            if (StackPanel1.Children.Count == 0)
                Hide();
        }
        private void Item_Dismissed(object? sender, EventArgs e)
        {
            RemoveItem((sender as IngestItem)!);
        }
    }
}
