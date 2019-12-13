using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using static dcim_ingester.VolumeWatcher;
using static dcim_ingester.Helpers;

namespace dcim_ingester
{
    public partial class MainWindow : Window
    {
        private List<IngesterTask> Tasks = new List<IngesterTask>();
        private List<string> Volumes = new List<string>();

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
                VolumeWatcher.RegisterDeviceNotification(source.Handle);
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

            if (msg == VolumeWatcher.WM_DEVICE_CHANGE)
            {
                switch ((int)wparam)
                {
                    case VolumeWatcher.DBT_DEVICE_ARRIVAL:
                        {
                            DevBroadcastDeviceInterface ver = (DevBroadcastDeviceInterface)
                                Marshal.PtrToStructure(lparam, typeof(DevBroadcastDeviceInterface));
                            if (!ver.Name.StartsWith("\\\\?\\")) break;

                            new Thread(delegate () { VolumeMounted(); }).Start();
                            break;
                        }

                    case VolumeWatcher.DBT_DEVICE_REMOVE_COMPLETE:
                        {
                            DevBroadcastDeviceInterface ver = (DevBroadcastDeviceInterface)
                                Marshal.PtrToStructure(lparam, typeof(DevBroadcastDeviceInterface));
                            if (!ver.Name.StartsWith("\\\\?\\")) break;

                            new Thread(delegate () { VolumeUnmounted(); }).Start();
                            break;
                        }
                }
            }

            handled = false;
            return IntPtr.Zero;
        }


        private void VolumeMounted()
        {
            Application.Current.Dispatcher.Invoke(delegate ()
            {
                List<string> newVolumes = GetVolumes();
                foreach (string deviceId in newVolumes)
                {
                    if (!Volumes.Contains(deviceId))
                    {
                        if (Directory.Exists(Path.Combine(GetVolumeLetter(deviceId), "DCIM")))
                            StartNewIngesterTask(deviceId);
                    }
                }

                Volumes = GetVolumes();
            });
        }
        private void VolumeUnmounted()
        {
            Application.Current.Dispatcher.Invoke(delegate ()
            {
                Volumes = GetVolumes();
            });
        }

        private void StartNewIngesterTask(string deviceId)
        {
            IngesterTask task = new IngesterTask(deviceId);
            task.Margin = new Thickness(0, 20, 0, 0);
            Tasks.Add(task);
            StackPanelTasks.Children.Add(task);

            Height += 140;
            Rect workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 20;
            Top = workArea.Bottom - Height - 20;
            Show();
        }
    }
}
