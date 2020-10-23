using DCIMIngester.Ingesting;
using DCIMIngester.Routines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using static DCIMIngester.Ingesting.IngestTask;
using static DCIMIngester.Routines.VolumeWatcher;

namespace DCIMIngester.Windows
{
    public partial class MainWindow : Window
    {
        private readonly VolumeWatcher VolumeWatcher = new VolumeWatcher();
        private readonly List<IngestTask> TasksInProgress = new List<IngestTask>();

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

            VolumeWatcher.VolumeAdded += VolumeWatcher_VolumeAdded;
            VolumeWatcher.VolumeRemoved += VolumeWatcher_VolumeRemoved;
            VolumeWatcher.StartWatching(HwndSource.FromHwnd(windowHandle));
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            VolumeWatcher.StopWatching();
        }

        private void VolumeWatcher_VolumeAdded(object sender, VolumeChangedEventArgs e)
        {
            if (Properties.Settings.Default.Endpoint == "" || ((App)Application.Current).IsSettingsOpen)
                return;

            if (Directory.Exists(Path.Combine(Helpers.GetVolumeLetter(e.VolumeID), "DCIM")))
            {
                IngestTask task = TasksInProgress.SingleOrDefault(ti => ti.Context.VolumeID == e.VolumeID);

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
        }
        private void VolumeWatcher_VolumeRemoved(object sender, VolumeChangedEventArgs e)
        {
            IngestTask task = TasksInProgress.SingleOrDefault(
                it => it.Context.VolumeID == e.VolumeID && it.Status == TaskStatus.Prompting);

            if (task != null)
                RemoveTask(task);
        }

        private void AddTask(IngestTaskContext taskContext)
        {
            IngestTask task = new IngestTask(taskContext);
            task.Dismissed += Task_Dismissed;
            task.Load();

            TasksInProgress.Add(task);
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
            TasksInProgress.Remove(task);
            StackPanelTasks.Children.Remove(task);

            Height -= 140;
            Left = SystemParameters.WorkArea.Right - Width - 20;
            Top = SystemParameters.WorkArea.Bottom - Height - 20;

            if (TasksInProgress.Count == 0)
                Hide();
        }
    }
}
