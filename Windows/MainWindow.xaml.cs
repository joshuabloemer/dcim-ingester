using DcimIngester.Ingesting;
using DcimIngester.Routines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using static DcimIngester.Ingesting.IngestTask;

namespace DcimIngester.Windows
{
    public partial class MainWindow : Window
    {
        private readonly VolumeWatcher volumeWatcher = new VolumeWatcher();
        private readonly List<IngestTask> tasksInProgress = new List<IngestTask>();

        public int TaskCount = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;

            // Hide window from the Windows task switcher by making it a tool window
            int extendedStyle = Helpers.GetWindowLong(windowHandle, Helpers.GWL_EXSTYLE);
            extendedStyle |= Helpers.WS_EX_TOOLWINDOW;
            Helpers.SetWindowLong(windowHandle, Helpers.GWL_EXSTYLE, extendedStyle);

            volumeWatcher.VolumeAdded += VolumeWatcher_VolumeAdded;
            volumeWatcher.VolumeRemoved += VolumeWatcher_VolumeRemoved;
            volumeWatcher.StartWatching(HwndSource.FromHwnd(windowHandle));
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            volumeWatcher.StopWatching();
        }

        private void VolumeWatcher_VolumeAdded(object sender, VolumeChangedEventArgs e)
        {
            if (Properties.Settings.Default.Destination == "" || ((App)Application.Current).IsSettingsOpen)
                return;

            if (Directory.Exists(Path.Combine(Helpers.GetVolumeLetter(e.VolumeID), "DCIM")))
            {
                Interlocked.Increment(ref TaskCount);
                IngestTask task = tasksInProgress.SingleOrDefault(ti => ti.Context.VolumeID == e.VolumeID);

                if (task != null)
                    RemoveTask(task);

                IngestTaskContext taskContext = new IngestTaskContext(e.VolumeID);
                taskContext.FileDiscoveryCompleted += TaskContext_FileDiscoveryCompleted;
                taskContext.DiscoverFiles();
            }
        }
        private void TaskContext_FileDiscoveryCompleted(object sender, FileDiscoveryCompletedEventArgs e)
        {
            if (e.Result == FileDiscoveryCompletedEventArgs.FileDiscoveryResult.FilesFound)
                AddTask(sender as IngestTaskContext);
            else Interlocked.Decrement(ref TaskCount);
        }
        private void VolumeWatcher_VolumeRemoved(object sender, VolumeChangedEventArgs e)
        {
            IngestTask task = tasksInProgress.SingleOrDefault(
                it => it.Context.VolumeID == e.VolumeID && it.Status == TaskStatus.Prompting);

            if (task != null)
                RemoveTask(task);
        }

        private void AddTask(IngestTaskContext taskContext)
        {
            IngestTask task = new IngestTask(taskContext);
            task.Dismissed += Task_Dismissed;
            task.Load();

            tasksInProgress.Add(task);
            StackPanelTasks.Children.Add(task);

            Height += 140;
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;
            Show();
        }
        private void Task_Dismissed(object sender, EventArgs e)
        {
            RemoveTask(sender as IngestTask);
        }
        private void RemoveTask(IngestTask task)
        {
            Interlocked.Decrement(ref TaskCount);

            tasksInProgress.Remove(task);
            StackPanelTasks.Children.Remove(task);

            Height -= 140;
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;

            if (tasksInProgress.Count == 0)
                Hide();
        }
    }
}
