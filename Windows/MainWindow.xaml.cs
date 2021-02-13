using DcimIngester.Controls;
using DcimIngester.Ingesting;
using DcimIngester.VolumeWatching;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace DcimIngester.Windows
{
    public partial class MainWindow : Window
    {
        private VolumeWatcher? volumeWatcher = null;
        private readonly List<IngestItem> items = new List<IngestItem>();

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
            int extendedStyle = Utilities.GetWindowLong(windowHandle, Utilities.GWL_EXSTYLE);
            extendedStyle |= Utilities.WS_EX_TOOLWINDOW;
            Utilities.SetWindowLong(windowHandle, Utilities.GWL_EXSTYLE, extendedStyle);

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
            if (Properties.Settings.Default.Destination == "" ||
                ((App)Application.Current).IsSettingsOpen)
            {
                return;
            }

            Interlocked.Increment(ref workCount);
            IngestItem? item = items.SingleOrDefault(i => i.VolumeID == e.VolumeID);

            if (item != null)
            {
                if (item.Status == IngestTaskStatus.Ingesting ||
                    item.Status == IngestTaskStatus.Failed)
                {
                    Interlocked.Decrement(ref workCount);
                    return;
                }
                else RemoveItem(item);
            }

            try
            {
                IngestWork work = new IngestWork(e.VolumeID);

                if (await work.DiscoverFilesAsync())
                    Application.Current.Dispatcher.Invoke(() => AddItem(work));
                else Interlocked.Decrement(ref workCount);
            }
            catch
            {
                Interlocked.Decrement(ref workCount);
            }
        }
        private void VolumeWatcher_VolumeRemoved(object? sender, VolumeChangedEventArgs e)
        {
            IngestItem? item = items.SingleOrDefault(
                i => i.VolumeID == e.VolumeID && i.Status == IngestTaskStatus.Ready);

            if (item != null)
                Application.Current.Dispatcher.Invoke(() => RemoveItem(item));
        }

        private void AddItem(IngestWork work)
        {
            IngestItem item = new IngestItem(work);
            item.Margin = new Thickness(0, 20, 0, 0);
            item.Dismissed += Item_Dismissed;

            items.Add(item);
            StackPanel1.Children.Add(item);

            Height += item.Height + 20;
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;
            Show();
        }
        private void RemoveItem(IngestItem item)
        {
            Interlocked.Decrement(ref workCount);

            items.Remove(item);
            StackPanel1.Children.Remove(item);

            Height -= item.Height - 20;

            if (items.Count == 0)
                Hide();
        }
        private void Item_Dismissed(object? sender, EventArgs e)
        {
            RemoveItem((sender as IngestItem)!);
        }
    }
}
