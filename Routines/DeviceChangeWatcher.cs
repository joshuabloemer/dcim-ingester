using System;
using System.Runtime.InteropServices;

namespace DcimIngester.Routines
{
    // Original code from https://stackoverflow.com/questions/1976573/using-registerdevicenotification-in-a-net-app
    public class DeviceChangeWatcher
    {
        public const int WM_DEVICE_CHANGE = 0x0219; // Event for some device changed
        public const int DBT_DEVICE_ARRIVAL = 0x8000; // Event for device added
        public const int DBT_DEVICE_REMOVE_COMPLETE = 0x8004; // Event for device removed

        private IntPtr NotificationHandle;


        public void RegisterDeviceNotification(IntPtr windowHandle, Guid deviceInterfaceClass)
        {
            // Specifies which type of device to send notifications for. Here
            // DeviceType is 5 which means notifications will be sent for all devices
            // that are part of a specific device interface class (GUID).
            // See NotificationFilter parameter of RegisterDeviceNotification docs
            DevBroadcastDeviceInterface dbdi = new DevBroadcastDeviceInterface
            {
                DeviceType = 5,
                Reserved = 0,
                ClassGuid = deviceInterfaceClass,
                Name = ""
            };

            dbdi.Size = Marshal.SizeOf(dbdi);
            IntPtr filter = Marshal.AllocHGlobal(dbdi.Size);
            Marshal.StructureToPtr(dbdi, filter, true);

            NotificationHandle = RegisterDeviceNotification(windowHandle, filter, 0);
        }
        public void UnregisterDeviceNotification()
        {
            UnregisterDeviceNotification(NotificationHandle);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DevBroadcastDeviceInterface
        {
            public int Size;
            public int DeviceType;
            public int Reserved;
            public Guid ClassGuid;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string Name;
        }
    }
}
