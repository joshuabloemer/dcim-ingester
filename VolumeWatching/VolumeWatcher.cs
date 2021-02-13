using System;
using System.Collections.Generic;
using System.Management;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using static DcimIngester.Utilities;

namespace DcimIngester.VolumeWatching
{
    /// <summary>
    /// Raises events when FAT volumes are mounted to or unmounted from the system.
    /// </summary>
    public class VolumeWatcher
    {
        /// <summary>
        /// The FAT volumes currently mounted to the system.
        /// </summary>
        private List<Guid> volumes = new List<Guid>();

        /// <summary>
        /// Indicates whether volume watching is in progress.
        /// </summary>
        public bool IsWatching { get; private set; } = false;

        /// <summary>
        /// The handle of the window to use to receive volume change notifications from the OS.
        /// </summary>
        private readonly HwndSource windowHandle;

        /// <summary>
        /// The notification handle returned from <see cref="RegisterDeviceNotification(IntPtr, IntPtr, int)"./>
        /// </summary>
        private IntPtr notifHandle;

        /// <summary>
        /// Used for locking to ensure volume change notifications are dealt with serially.
        /// </summary>
        private readonly object updateLock = new object();

        /// <summary>
        /// Occurs when a FAT volume is mounted to the system.
        /// </summary>
        public event EventHandler<VolumeChangedEventArgs>? VolumeAdded;

        /// <summary>
        /// Occurs when a FAT volume is unmounted from the system.
        /// </summary>
        public event EventHandler<VolumeChangedEventArgs>? VolumeRemoved;

        /// <summary>
        /// Initialises a new instance of the <see cref="VolumeWatcher"/> class.
        /// </summary>
        /// <param name="windowHandle">The handle of the window to use to receive volume change notifications from the
        /// OS.</param>
        public VolumeWatcher(HwndSource windowHandle)
        {
            this.windowHandle = windowHandle;
        }

        /// <summary>
        /// Starts watching for changes to the FAT volumes mounted to the system.
        /// </summary>
        public void StartWatching()
        {
            if (IsWatching)
            {
                throw new InvalidOperationException(
                    "Cannot start volume watching because it has already started.");
            }

            IsWatching = true;

            // Create a struct to denote the type of device change messages we want to receive. Here
            // we want to receive messages about volume devices.
            DevBroadcastDeviceInterface dbdi = new DevBroadcastDeviceInterface(GUID_DEVINTERFACE_VOLUME);
            dbdi.Size = Marshal.SizeOf(dbdi);

            // Tell Windows to send us device change messages
            IntPtr filter = Marshal.AllocHGlobal(dbdi.Size);
            Marshal.StructureToPtr(dbdi, filter, true);
            notifHandle = RegisterDeviceNotification(windowHandle.Handle, filter, 0);

            volumes = GetVolumes();
            windowHandle.AddHook(WndProc);
        }

        /// <summary>
        /// Stops watching for changes to the FAT volumes mounted to the system.
        /// </summary>
        public void StopWatching()
        {
            if (!IsWatching)
                throw new InvalidOperationException("Cannot stop volume watching because it has not started.");

            UnregisterDeviceNotification(notifHandle);
            windowHandle.RemoveHook(WndProc);
            IsWatching = false;
        }

        /// <summary>
        /// Invoked whenever the window identified by <see cref="windowHandle"/> receives a message. Reacts to messages
        /// that indicate the volumes mounted to the system have changed.
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam, ref bool handled)
        {
            // Only react to device addition and removal messages
            if (msg == WM_DEVICE_CHANGE &&
                ((int)wparam == DBT_DEVICE_ARRIVAL || (int)wparam == DBT_DEVICE_REMOVE_COMPLETE))
            {
                DevBroadcastDeviceInterface dbdi = (DevBroadcastDeviceInterface)
                    Marshal.PtrToStructure(lparam, typeof(DevBroadcastDeviceInterface))!;

                // Two messages are received each time but only one contains valid data
                if (dbdi.Name.StartsWith("\\\\?\\"))
                {
                    // Handle message in a new thread so this method can return quickly
                    new Thread(() =>
                    {
                        lock (updateLock)
                            UpdateVolumes();
                    }).Start();
                }
            }

            handled = false;
            return IntPtr.Zero;
        }

        /// <summary>
        /// Updates the list of currently mounted volumes and invokes the added or removed event for each difference
        /// between the old and new lists.
        /// </summary>
        private void UpdateVolumes()
        {
            List<Guid> newVolumes = GetVolumes();

            // Check for any added volumes
            foreach (Guid volume in newVolumes)
            {
                if (!volumes.Contains(volume))
                    VolumeAdded?.Invoke(this, new VolumeChangedEventArgs(volume));
            }

            // Check for any removed volumes
            foreach (Guid volume in volumes)
            {
                if (!newVolumes.Contains(volume))
                    VolumeRemoved?.Invoke(this, new VolumeChangedEventArgs(volume));
            }

            volumes = newVolumes;
        }

        /// <summary>
        /// Gets the volumes currently mounted to the system.
        /// </summary>
        /// <returns>The GUIDs of the volumes mounted to the system that have a drive letter, are formatted with FAT12,
        /// FAT16, FAT32 or exFAT, and are not boot volumes.</returns>
        private static List<Guid> GetVolumes()
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher(
                "SELECT DeviceID FROM Win32_Volume " +
                    "WHERE DriveLetter != NULL AND " +
                        "(FileSystem = 'FAT12' OR FileSystem = 'FAT16' " +
                            "OR FileSystem = 'FAT32' OR FileSystem = 'exFAT')" +
                        "AND BootVolume = False");

            List<Guid> volumes = new List<Guid>();

            foreach (ManagementObject volume in query.Get())
            {
                string volumeId = volume["DeviceID"].ToString()!;

                volumeId = volumeId.Substring(
                    volumeId.IndexOf('{'), volumeId.IndexOf('}') - volumeId.IndexOf('{') + 1);

                volumes.Add(new Guid(volumeId));
            }

            return volumes;
        }
    }
}