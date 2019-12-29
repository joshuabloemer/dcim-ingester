using DCIMIngester.Ingester;
using DCIMIngester.Routines;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using static DCIMIngester.Ingester.IngesterTask;
using static DCIMIngester.Routines.Helpers;
using static DCIMIngester.Routines.VolumeWatcher;

namespace DCIMIngester.Windows
{
    public partial class MainWindow : Window
    {
        private readonly VolumeWatcher Volumes = new VolumeWatcher();

        private readonly List<IngesterTask> tasks = new List<IngesterTask>();
        public IReadOnlyCollection<IngesterTask> Tasks
        {
            get { return tasks.AsReadOnly(); }
        }

        public MainWindow()
        {
            InitializeComponent();
        }


        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource windowHandle
                = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);

            // Register for device change messages
            if (windowHandle != null)
            {
                Volumes.VolumeAdded += Devices_VolumeAdded;
                Volumes.VolumeRemoved += Devices_VolumeRemoved;
                Volumes.StartWatching(windowHandle);
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var windowHandle = new WindowInteropHelper(this).Handle;

            int GWL_EX_STYLE = -20;
            int WS_EX_APPWINDOW = 0x00040000;
            int WS_EX_TOOLWINDOW = 0x00000080;

            // Hide the window from windows task switcher
            SetWindowLong(windowHandle, GWL_EX_STYLE,
                    (GetWindowLong(windowHandle, GWL_EX_STYLE) | WS_EX_TOOLWINDOW)
                    & ~WS_EX_APPWINDOW);
        }

        private void Devices_VolumeAdded(object sender, VolumeChangedEventArgs e)
        {
            if (!IsLoaded) return;
            if (Properties.Settings.Default.Endpoint == null ||
                Properties.Settings.Default.Endpoint == "") return;
            if (((App)Application.Current).IsSettingsOpen) return;

            // Dismiss any non-dismissed left over task for this volume
            foreach (IngesterTask task in new List<IngesterTask>(Tasks))
            {
                if (task.Volume == e.VolumeID)
                {
                    StopIngesterTask(task);
                    break;
                }
            }

            string volumeLetter = GetVolumeLetter(e.VolumeID);
            if (Directory.Exists(Path.Combine(volumeLetter, "DCIM")))
                StartIngesterTask(e.VolumeID);
        }
        private void Devices_VolumeRemoved(object sender, VolumeChangedEventArgs e)
        {
            if (!IsLoaded) return;
            foreach (IngesterTask task in Tasks)
            {
                if (task.Volume == e.VolumeID && task.Status == TaskStatus.Waiting)
                {
                    StopIngesterTask(task);
                    break;
                }
            }
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            Volumes.StopWatching();
        }

        private void StartIngesterTask(Guid volume)
        {
            Height += 140;
            Rect workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 20;
            Top = workArea.Bottom - Height - 20;
            Show();

            IngesterTask task = new IngesterTask(volume);
            task.TaskDismissed += Task_TaskDismissed;

            tasks.Add(task);
            StackPanelTasks.Children.Add(task);
            task.DiscoverFiles();
        }
        private void Task_TaskDismissed(object sender, TaskDismissEventArgs e)
        {
            StopIngesterTask(e.Task);
        }
        private void StopIngesterTask(IngesterTask task)
        {
            StackPanelTasks.Children.Remove(task);
            tasks.Remove(task);

            Height -= 140;
            Rect workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 20;
            Top = workArea.Bottom - Height - 20;

            if (Tasks.Count == 0) Hide();
        }


        [DllImport("user32.dll", SetLastError = true)]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    }
}
