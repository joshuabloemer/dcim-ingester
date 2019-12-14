using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using static dcim_ingester.Helpers;
using static dcim_ingester.VolumeWatcher;

namespace dcim_ingester
{
    public partial class MainWindow : Window
    {
        private List<IngesterTask> Tasks = new List<IngesterTask>();
        private List<Guid> Volumes = new List<Guid>();

        ConcurrentQueue<object> Messages = new ConcurrentQueue<object>();
        bool isHandlingMessage = false;

        public MainWindow()
        {
            InitializeComponent();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Volumes = GetVolumes();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            // Register for device change messages
            HwndSource source = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
            if (source != null)
            {
                source.AddHook(WindowMessageHandler);
                RegisterDeviceNotification(source.Handle);
            }
        }
        private IntPtr WindowMessageHandler(
            IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            if (!IsLoaded)
            {
                handled = false;
                return IntPtr.Zero;
            }

            // Only want to receive device insertion and removal messages
            if (msg == WM_DEVICE_CHANGE &&
                (int)wparam == DBT_DEVICE_ARRIVAL || (int)wparam == DBT_DEVICE_REMOVE_COMPLETE)
            {
                DevBroadcastDeviceInterface ver = (DevBroadcastDeviceInterface)
                    Marshal.PtrToStructure(lparam, typeof(DevBroadcastDeviceInterface));

                if (ver.Name.StartsWith("\\\\?\\")) // Ignore invalid false positives
                {
                    // When we get any message, keep track of it and then process it
                    Messages.Enqueue(null);
                    if (!isHandlingMessage)
                    {
                        isHandlingMessage = true;
                        new Thread(delegate () { VolumesChanged(); }).Start();
                    }
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        private void VolumesChanged()
        {
            List<Guid> newVolumes = GetVolumes();

            // Check for any added volumes
            foreach (Guid volume in newVolumes)
            {
                if (!Volumes.Contains(volume))
                {
                    if (Directory.Exists(Path.Combine(GetVolumeLetter(volume), "DCIM")))
                    {
                        Application.Current.Dispatcher.Invoke(delegate ()
                        { StartIngesterTask(volume); });
                    }
                }
            }

            // Check for any removed volumes
            foreach (Guid volume in Volumes)
            {
                if (!newVolumes.Contains(volume))
                {
                    Application.Current.Dispatcher.Invoke(delegate ()
                    { StopIngesterTask(volume); });
                }
            }

            Volumes = newVolumes;

            // Could have received another message during previous message processing
            if (Messages.Count == 0)
                isHandlingMessage = false;
            else VolumesChanged();
        }

        private void StartIngesterTask(Guid volumeId)
        {
            IngesterTask task = new IngesterTask(volumeId);
            task.Margin = new Thickness(0, 20, 0, 0);
            Tasks.Add(task);
            StackPanelTasks.Children.Add(task);

            Height += 140;
            Rect workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 20;
            Top = workArea.Bottom - Height - 20;
            Show();
        }
        private void StopIngesterTask(Guid volumeId)
        {
            IngesterTask task = Tasks.FirstOrDefault(t => t.VolumeID == volumeId);
            if (task != null)
            {
                StackPanelTasks.Children.Remove(task);
                Tasks.Remove(task);

                if (Tasks.Count == 0) Hide();
            }
        }
    }
}
