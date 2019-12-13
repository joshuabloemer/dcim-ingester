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
            Rect workingArea = SystemParameters.WorkArea;
            Left = workingArea.Right - Width - 20;
            Top = workingArea.Bottom - Height - 20;

            Volumes = GetVolumes();
            PrintVolumes(Volumes);
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
                foreach (string volume in newVolumes)
                {
                    if (!Volumes.Contains(volume))
                    {
                        string driveLetter = GetDriveLetter(volume);
                        Console.WriteLine("new -- " + driveLetter);

                        if (Directory.Exists(Path.Combine(driveLetter, "DCIM")))
                            StartNewTask(driveLetter);
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

        private void StartNewTask(string driveLetter)
        {
            IngesterTask task = new IngesterTask(driveLetter);
            task.Margin = new Thickness(0, 20, 0, 0);
            Tasks.Add(task);
            StackPanelTasks.Children.Add(task);

            Height += 140;
            Top -= 140;
        }

        private List<string> GetVolumes()
        {
            List<string> volumes = new List<string>();
            ManagementObjectSearcher query = new ManagementObjectSearcher(
                "SELECT DeviceID FROM Win32_Volume WHERE DriveLetter != NULL");

            foreach (ManagementObject volume in query.Get())
            {
                // Get GUID only to avoid a mess with slashes and escaping
                string deviceId = volume["DeviceID"].ToString();
                deviceId = deviceId.Substring(deviceId.IndexOf('{'),
                    deviceId.IndexOf('}') - deviceId.IndexOf('{') + 1);
                volumes.Add(deviceId);
            }

            return volumes;
        }
        private string GetDriveLetter(string deviceId)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher(string.Format(
                "SELECT DriveLetter FROM Win32_Volume WHERE DeviceID LIKE '%{0}%'", deviceId));
            ManagementObjectCollection result = query.Get();

            if (result.Count > 0)
                return result.OfType<ManagementObject>().First()["DriveLetter"].ToString();
            return null;
        }

        private void PrintVolumes(List<string> volumes)
        {
            Console.WriteLine("VOLUMES:");

            foreach (string s in volumes)
                Console.WriteLine(GetDriveLetter(s) + " -- " + s);
        }
    }
}
