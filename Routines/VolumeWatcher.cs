using System;
using System.Runtime.InteropServices;

namespace dcim_ingester.Routines
{
    // Taken from https://stackoverflow.com/questions/1976573/using-registerdevicenotification-in-a-net-app
    public static class VolumeWatcher
    {
        public const int WM_DEVICE_CHANGE = 0x0219; // Event for some device changed
        public const int DBT_DEVICE_ARRIVAL = 0x8000; // Event for device arrival
        public const int DBT_DEVICE_REMOVE_COMPLETE = 0x8004; // Event for device removal

        private const int DBT_DEVICE_TYPE_DEVICE_INTERFACE = 5;

        private static readonly Guid GUID_DEVICE_INTERFACE_VOLUME
            = new Guid("53F5630D-B6BF-11D0-94F2-00A0C91EFB8B"); // Volume device group

        private static IntPtr NotificationHandle;


        public static void RegisterDeviceNotification(IntPtr windowHandle)
        {
            DevBroadcastDeviceInterface dbi = new DevBroadcastDeviceInterface
            {
                DeviceType = DBT_DEVICE_TYPE_DEVICE_INTERFACE,
                Reserved = 0,
                ClassGuid = GUID_DEVICE_INTERFACE_VOLUME,
                Name = ""
            };

            dbi.Size = Marshal.SizeOf(dbi);
            IntPtr buffer = Marshal.AllocHGlobal(dbi.Size);
            Marshal.StructureToPtr(dbi, buffer, true);

            NotificationHandle = RegisterDeviceNotification(windowHandle, buffer, 0);
        }
        public static void UnregisterDeviceNotification()
        {
            UnregisterDeviceNotification(NotificationHandle);
        }


        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(
            IntPtr recipient, IntPtr notificationFilter, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterDeviceNotification(IntPtr handle);

        // Taken from https://stackoverflow.com/questions/2208722/how-to-get-friendly-device-name-from-dev-broadcast-deviceinterface-and-device-ins
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