using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Interop;
using static DcimIngester.Utilities;

namespace DcimIngester.VolumeWatching
{
    /// <summary>
    /// Raises events when removable volumes are mounted to or unmounted from the system.
    /// </summary>
    public class VolumeWatcher
    {
        /// <summary>
        /// Indicates whether volume watching is in progress.
        /// </summary>
        public bool IsWatching { get; private set; } = false;

        /// <summary>
        /// The handle of the window to use to receive volume change notifications.
        /// </summary>
        private readonly HwndSource windowHandle;

        /// <summary>
        /// The notification handle returned from
        /// <see cref="NativeMethods.RegisterDeviceNotification(IntPtr, IntPtr, int)"/>.
        /// </summary>
        private IntPtr notifHandle;

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
        /// <param name="windowHandle">The handle of the window to use to receive volume change notifications.</param>
        public VolumeWatcher(HwndSource windowHandle)
        {
            this.windowHandle = windowHandle;
        }

        /// <summary>
        /// Starts watching for changes to the removable volumes mounted to the system.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if volume watching has already started.</exception>
        public void StartWatching()
        {
            if (IsWatching)
            {
                throw new InvalidOperationException(
                    "Cannot start volume watching because it has already started.");
            }

            IsWatching = true;

            // Specify the type of device change messages we want to receive. Here we want to
            // receive messages about volume devices.
            NativeMethods.DEV_BROADCAST_DEVICEINTERFACE dbdi =
                new NativeMethods.DEV_BROADCAST_DEVICEINTERFACE()
                {
                    dbcc_devicetype = NativeMethods.DBT_DEVTYP_DEVICEINTERFACE,
                    dbcc_classguid = NativeMethods.GUID_DEVINTERFACE_VOLUME
                };

            dbdi.dbcc_size = Marshal.SizeOf(dbdi);

            // Tell Windows to send us device change messages
            IntPtr filter = Marshal.AllocHGlobal(dbdi.dbcc_size);
            Marshal.StructureToPtr(dbdi, filter, true);
            notifHandle = NativeMethods.RegisterDeviceNotification(windowHandle.Handle, filter, 0);

            windowHandle.AddHook(WndProc);
        }

        /// <summary>
        /// Stops watching for changes to the removable volumes mounted to the system.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if volume watching has already stopped.</exception>
        public void StopWatching()
        {
            if (!IsWatching)
            {
                throw new InvalidOperationException(
                    "Cannot stop volume watching because it has not started.");
            }

            NativeMethods.UnregisterDeviceNotification(notifHandle);
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
            if (msg == NativeMethods.WM_DEVICECHANGE &&
                ((int)wparam == NativeMethods.DBT_DEVICEARRIVAL ||
                (int)wparam == NativeMethods.DBT_DEVICEREMOVECOMPLETE))
            {
                NativeMethods.DEV_BROADCAST_HDR header = (NativeMethods.DEV_BROADCAST_HDR)
                    Marshal.PtrToStructure(lparam, typeof(NativeMethods.DEV_BROADCAST_HDR))!;

                // Only react to volume devices
                if (header.dbch_devicetype == NativeMethods.DBT_DEVTYP_VOLUME)
                {
                    NativeMethods.DEV_BROADCAST_VOLUME volume = (NativeMethods.DEV_BROADCAST_VOLUME)
                        Marshal.PtrToStructure(lparam, typeof(NativeMethods.DEV_BROADCAST_VOLUME))!;

                    string volumeLetter = UnitMaskToDriveLetter(volume.dbcv_unitmask) + ":";

                    // Invoke events in new thread to allow WndProc to return quickly
                    new Thread(() =>
                    {
                        if ((int)wparam == NativeMethods.DBT_DEVICEARRIVAL)
                            VolumeAdded?.Invoke(this, new VolumeChangedEventArgs(volumeLetter));
                        else VolumeRemoved?.Invoke(this, new VolumeChangedEventArgs(volumeLetter));
                    }).Start();
                }
            }

            handled = false;
            return IntPtr.Zero;
        }
    }
}