using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace DcimIngester.Routines
{
    // Original code from https://stackoverflow.com/questions/1976573/using-registerdevicenotification-in-a-net-app
    public class VolumeWatcher
    {
        private DeviceChangeWatcher DeviceWatcher = new DeviceChangeWatcher();

        private List<Guid> volumes = new List<Guid>();
        public IReadOnlyCollection<Guid> Volumes
        {
            get { return volumes.AsReadOnly(); }
        }

        private int MessagesToProcess = 0;
        bool isHandlingMessage = false;

        public event EventHandler<VolumeChangedEventArgs> VolumeAdded;
        public event EventHandler<VolumeChangedEventArgs> VolumeRemoved;

        private object MessageHandleLock = new object();

        public VolumeWatcher()
        {

        }


        public void StartWatching(HwndSource windowHandle)
        {
            windowHandle.AddHook(WindowProcedure);
            volumes = GetVolumes();

            DeviceWatcher.RegisterDeviceNotification(
                windowHandle.Handle, new Guid("53F5630D-B6BF-11D0-94F2-00A0C91EFB8B")); // Volume devices
        }
        public void StopWatching()
        {
            DeviceWatcher.UnregisterDeviceNotification();
            volumes = null;
        }

        private IntPtr WindowProcedure(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            // Only process device addition and removal messages
            if (msg == DeviceChangeWatcher.WM_DEVICE_CHANGE && ((int)wparam == DeviceChangeWatcher.DBT_DEVICE_ARRIVAL
                || (int)wparam == DeviceChangeWatcher.DBT_DEVICE_REMOVE_COMPLETE))
            {
                DeviceChangeWatcher.DevBroadcastDeviceInterface dbdi = (DeviceChangeWatcher.DevBroadcastDeviceInterface)
                    Marshal.PtrToStructure(lparam, typeof(DeviceChangeWatcher.DevBroadcastDeviceInterface));

                if (dbdi.Name.StartsWith("\\\\?\\")) // Ignore invalid false positives
                {
                    lock (MessageHandleLock)
                    {
                        MessagesToProcess++;
                        if (!isHandlingMessage)
                        {
                            isHandlingMessage = true;

                            // Handle message in new thread to allow this method to return
                            new Thread(() => VolumesChanged()).Start();
                        }
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
                    Application.Current.Dispatcher.Invoke(() =>
                        VolumeAdded?.Invoke(this, new VolumeChangedEventArgs(volume)));
                }
            }

            // Check for any removed devices
            foreach (Guid volume in Volumes)
            {
                if (!newVolumes.Contains(volume))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                        VolumeRemoved?.Invoke(this, new VolumeChangedEventArgs(volume)));
                }
            }

            volumes = newVolumes;
            lock (MessageHandleLock)
            {
                MessagesToProcess--;

                // Could have received another message while processing this message
                if (MessagesToProcess == 0)
                {
                    isHandlingMessage = false;
                    return;
                }
            }

            VolumesChanged();
        }

        private static List<Guid> GetVolumes()
        {
            List<Guid> volumes = new List<Guid>();
            ManagementObjectSearcher query = new ManagementObjectSearcher(
                "SELECT DeviceID FROM Win32_Volume WHERE DriveLetter != NULL "
                + "AND (FileSystem = 'FAT12' OR FileSystem = 'FAT16' OR FileSystem = 'FAT32' "
                + "OR FileSystem = 'exFAT') AND BootVolume = False");

            foreach (ManagementObject volume in query.Get())
            {
                // Extract GUID to avoid a mess with slashes and escaping
                string volumeId = volume["DeviceID"].ToString();
                volumeId = volumeId.Substring(volumeId.IndexOf('{'), volumeId.IndexOf('}') - volumeId.IndexOf('{') + 1);
                volumes.Add(new Guid(volumeId));
            }

            return volumes;
        }


        public class VolumeChangedEventArgs : EventArgs
        {
            public Guid VolumeID { get; private set; }

            public VolumeChangedEventArgs(Guid volumeId)
            {
                VolumeID = volumeId;
            }
        }
    }
}