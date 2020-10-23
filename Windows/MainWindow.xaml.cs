using DCIMIngester.Ingesting;
using DCIMIngester.Routines;
using System;
using System.Collections.Generic;
using System.IO;
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

        private readonly List<IngestTaskContext> TasksInDiscovery = new List<IngestTaskContext>();
        private readonly List<IngestTask> TasksInProgress = new List<IngestTask>();


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            int GWL_EX_STYLE = -20;
            int WS_EX_APPWINDOW = 0x00040000;
            int WS_EX_TOOLWINDOW = 0x00000080;

            IntPtr windowHandlePtr = new WindowInteropHelper(this).Handle;

            // Hide this window from the Windows task switcher
            SetWindowLong(windowHandlePtr, GWL_EX_STYLE,
                (GetWindowLong(windowHandlePtr, GWL_EX_STYLE) | WS_EX_TOOLWINDOW) & ~WS_EX_APPWINDOW);

            HwndSource windowHandle = HwndSource.FromHwnd(windowHandlePtr);
            VolumeWatcher.VolumeAdded += VolumeWatcher_VolumeAdded;
            VolumeWatcher.VolumeRemoved += VolumeWatcher_VolumeRemoved;
            VolumeWatcher.StartWatching(windowHandle);
        }

        private void VolumeWatcher_VolumeAdded(object sender, VolumeChangedEventArgs e)
        {
            if (Properties.Settings.Default.Endpoint == "" || ((App)Application.Current).IsSettingsOpen)
                return;

            // Remove any leftover finished tasks for this volume
            foreach (IngestTask task in new List<IngestTask>(TasksInProgress))
            {
                if (task.Context.VolumeID == e.VolumeID && (task.Status == TaskStatus.Completed ||
                    task.Status == TaskStatus.Failed || task.Status == TaskStatus.Cancelled))
                {
                    RemoveTask(task);
                    break;
                }
            }

            if (Directory.Exists(Path.Combine(Helpers.GetVolumeLetter(e.VolumeID), "DCIM")))
            {
                IngestTaskContext context = new IngestTaskContext(e.VolumeID);
                context.FileDiscoveryCompleted += Context_FileDiscoveryCompleted;
                context.DiscoverFiles();
            }
        }
        private void Context_FileDiscoveryCompleted(object sender, FileDiscoveryCompletedEventArgs e)
        {
            TasksInDiscovery.Remove(sender as IngestTaskContext);
            (sender as IngestTaskContext).FileDiscoveryCompleted -= Context_FileDiscoveryCompleted;

            if (e.Result == FileDiscoveryCompletedEventArgs.FileDiscoveryResult.FilesFound)
                AddTask(sender as IngestTaskContext);
        }
        private void Task_Dismissed(object sender, EventArgs e)
        {
            RemoveTask(sender as IngestTask);
        }
        private void VolumeWatcher_VolumeRemoved(object sender, VolumeChangedEventArgs e)
        {
            // If a task exists for this volume in the prompting state, remove it
            foreach (IngestTask task in TasksInProgress)
            {
                if (task.Context.VolumeID == e.VolumeID && task.Status == TaskStatus.Prompting)
                {
                    RemoveTask(task);
                    break;
                }
            }
        }

        private void AddTask(IngestTaskContext context)
        {
            IngestTask task = new IngestTask(context);
            task.Dismissed += Task_Dismissed;
            task.Start();
            TasksInProgress.Add(task);
            StackPanelTasks.Children.Add(task);

            Height += 140;
            Rect workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 20;
            Top = workArea.Bottom - Height - 20;
            Show();
        }
        private void RemoveTask(IngestTask task)
        {
            StackPanelTasks.Children.Remove(task);
            TasksInProgress.Remove(task);

            Height -= 140;
            Rect workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 20;
            Top = workArea.Bottom - Height - 20;

            if (TasksInProgress.Count == 0) Hide();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            VolumeWatcher.StopWatching();
        }


        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
