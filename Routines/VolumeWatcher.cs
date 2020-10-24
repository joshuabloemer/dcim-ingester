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
    public class VolumeWatcher
    {
        private List<Guid> volumes = new List<Guid>();
        public IReadOnlyCollection<Guid> Volumes
        {
            get { return volumes.AsReadOnly(); }
        }

        public event EventHandler<VolumeChangedEventArgs> VolumeAdded;
        public event EventHandler<VolumeChangedEventArgs> VolumeRemoved;

        private IntPtr notificationHandle;
        private object changesLock = new object();

        /// <summary>
        /// Begins watching for changes to the volumes mounted to the system.
        /// </summary>
        /// <param name="window">The handle of the window to use to receive the notifications of changes.</param>
        public void StartWatching(HwndSource window)
        {
            // Create a struct to denote the type of device change messages we want to receive.
            // The type of messages is set by specifying a GUID that identifies a class of devices.
            // Here we want messages about devices that belong to the volume device class.
            DevBroadcastDeviceInterface dbdi = new DevBroadcastDeviceInterface(GUID_DEVINTERFACE_VOLUME);
            dbdi.Size = Marshal.SizeOf(dbdi);

            // Tell Windows to send us the device change messages specified above
            IntPtr filter = Marshal.AllocHGlobal(dbdi.Size);
            Marshal.StructureToPtr(dbdi, filter, true);
            notificationHandle = RegisterDeviceNotification(window.Handle, filter, 0);

            volumes = GetVolumes();
            window.AddHook(WindowProcedure);
        }

        /// <summary>
        /// Stops watching for changes to the volumes mounted to the system.
        /// </summary>
        public void StopWatching()
        {
            UnregisterDeviceNotification(notificationHandle);
            volumes = null;
        }

        /// <summary>
        /// Called whenever a window message is received. Reacts to messages that indicate the volumes mounted to the system have changed.
        /// </summary>
        /// <param name="hwnd">The handle of the window that received the message.</param>
        /// <param name="msg">The ID of the message.</param>
        /// <param name="wparam">The wParam value of the message.</param>
        /// <param name="lparam">The lParam value of the message.</param>
        /// <param name="handled">A value that indicates whether the message was handled.</param>
        /// <returns></returns>
        private IntPtr WindowProcedure(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            // Only process device addition and removal messages
            if (msg == WM_DEVICE_CHANGE &&
                ((int)wparam == DBT_DEVICE_ARRIVAL || (int)wparam == DBT_DEVICE_REMOVE_COMPLETE))
            {
                DevBroadcastDeviceInterface dbdi = (DevBroadcastDeviceInterface)
                    Marshal.PtrToStructure(lparam, typeof(DevBroadcastDeviceInterface));

                if (dbdi.Name.StartsWith("\\\\?\\"))
                {
                    // Handle message in a new thread to allow this method to return
                    new Thread(() => CheckVolumeChanges()).Start();
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        /// <summary>
        /// Compares the currently mounted volumes with the existing <see cref="Volumes"/> list and fires the appropriate event for any changes found.
        /// </summary>
        private void CheckVolumeChanges()
        {
            lock (changesLock)
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

                // Check for any removed volumes
                foreach (Guid volume in Volumes)
                {
                    if (!newVolumes.Contains(volume))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                            VolumeRemoved?.Invoke(this, new VolumeChangedEventArgs(volume)));
                    }
                }

                volumes = newVolumes;
            }
        }

        /// <summary>
        /// Gets the volumes mounted to the system that have a drive letter, are formatted with FAT12, FAT16, FAT32 or exFAT, and are not boot volumes.
        /// </summary>
        /// <returns>The GUIDs of the volumes.</returns>
        private static List<Guid> GetVolumes()
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher("SELECT DeviceID FROM Win32_Volume " +
                "WHERE DriveLetter != NULL AND (FileSystem = 'FAT12' OR FileSystem = 'FAT16' " +
                "OR FileSystem = 'FAT32' OR FileSystem = 'exFAT') AND BootVolume = False");

            List<Guid> volumes = new List<Guid>();

            foreach (ManagementObject volume in query.Get())
            {
                string volumeId = volume["DeviceID"].ToString();

                volumeId = volumeId.Substring(
                    volumeId.IndexOf('{'), volumeId.IndexOf('}') - volumeId.IndexOf('{') + 1);

                volumes.Add(new Guid(volumeId));
            }

            return volumes;
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr filter, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr recipient);

        /// <summary>
        /// Represents information related to a WM_DEVICECHANGE message. This struct is actually a DEV_BROADCAST_HDR, but when <see cref="DeviceType"/> 
        /// is 5 it takes on the <see cref="ClassGuid"/> and <see cref="Name"/> fields and becomes a DEV_BROADCAST_DEVICEINTERFACE.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DevBroadcastDeviceInterface
        {
            public int Size;
            public int DeviceType;
            public int Reserved;
            public Guid ClassGuid;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string Name;

            /// <summary>
            /// Initialises a new DevBroadcastDeviceInterface with a specific device interface class.
            /// </summary>
            /// <param name="classGuid">The device interface class. This specifies the class of devices that will trigger the message.</param>
            public DevBroadcastDeviceInterface(Guid classGuid)
            {
                Size = 0;
                DeviceType = DBT_DEVTYP_DEVICEINTERFACE;
                Reserved = 0;
                ClassGuid = classGuid;
                Name = "";
            }
        }

        private readonly Guid GUID_DEVINTERFACE_VOLUME = new Guid("53F5630D-B6BF-11D0-94F2-00A0C91EFB8B");
        private const int DBT_DEVTYP_DEVICEINTERFACE = 5;

        private const int WM_DEVICE_CHANGE = 0x0219;
        private const int DBT_DEVICE_ARRIVAL = 0x8000;
        private const int DBT_DEVICE_REMOVE_COMPLETE = 0x8004;
    }
}