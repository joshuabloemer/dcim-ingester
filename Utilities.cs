using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;

namespace DcimIngester
{
    public static class Utilities
    {
        /// <summary>
        /// Gets the letter of a volume that is mounted to the system.
        /// </summary>
        /// <param name="volumeId">The GUID of the volume.</param>
        /// <returns>The volume letter followed by a colon, or <see langword="null"/> if the volume was not found or has
        /// no drive letter.</returns>
        public static string? GetVolumeLetter(Guid volumeId)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher(string.Format(
                "SELECT DriveLetter FROM Win32_Volume WHERE DeviceID LIKE '%{0}%'", volumeId));
            ManagementObjectCollection result = query.Get();

            return result.OfType<ManagementObject>().SingleOrDefault()?["DriveLetter"]?.ToString();
        }

        /// <summary>
        /// Gets the label of a volume that is mounted to the system.
        /// </summary>
        /// <param name="volumeId">The GUID of the volume.</param>
        /// <returns>The volume label, <see cref="string.Empty"/> if the volume has no label, or <see langword="null"/>
        /// if the volume was not found.</returns>
        public static string? GetVolumeLabel(Guid volumeId)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher(string.Format(
                "SELECT Label FROM Win32_Volume WHERE DeviceID LIKE '%{0}%'", volumeId));
            ManagementObjectCollection result = query.Get();

            ManagementObject? row = result.OfType<ManagementObject>().SingleOrDefault();

            if (row != null)
                return row["Label"] != null ? row["Label"].ToString() : "";
            else return null;
        }

        /// <summary>
        /// Formats a numerical storage size into a string with units based on the magnitude of the value.
        /// </summary>
        /// <param name="bytes">The storage size in bytes.</param>
        /// <returns>The formatted storage size with units based on the magnitude of the value.</returns>
        public static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double bytesDouble = bytes;

            int i;
            for (i = 0; i < units.Length && bytes >= 1024; i++, bytes /= 1024)
                bytesDouble = bytes / 1024.0;

            return string.Format("{0:0.#} {1}", bytesDouble, units[i]);
        }

        /// <summary>
        /// Checks if a directory exists. Differs from <see cref="Directory.Exists(string)"/> by throwing an exception
        /// if there is an error.
        /// </summary>
        /// <param name="path">The directory to check.</param>
        /// <returns><see langword="true"/> if the directory exists, <see langword="false"/> if it does not exist.</returns>
        public static bool DirectoryExists(string path)
        {
            // GetCreationTime() returns the below time if the directory does not exist and throws
            // an exception if there was an error
            return Directory.GetCreationTime(path) != new DateTime(1601, 1, 1, 0, 0, 0);
        }

        /// <summary>
        /// Checks if a file exists. Differs from <see cref="File.Exists(string)"/> by throwing an exception if there
        /// is an error.
        /// </summary>
        /// <param name="path">The file to check.</param>
        /// <returns><see langword="true"/> if the file exists, <see langword="false"/> if it does not exist.</returns>
        public static bool FileExists(string path)
        {
            // GetCreationTime() returns the below time if the file does not exist and throws an
            // exception if there was an error
            return File.GetCreationTime(path) != new DateTime(1601, 1, 1, 0, 0, 0);
        }

        /// <summary>
        /// Copies a file to a directory. If the file already exists in the directory then a counter is added to the
        /// file name.
        /// </summary>
        /// <param name="sourcePath">The file to copy.</param>
        /// <param name="destDirectory">The directory to copy the file to.</param>
        /// <param name="renamed">Indicates whether the file name was changed to avoid duplication in the
        /// destination.</param>
        public static void CopyFile(string sourcePath, string destDirectory, out bool renamed)
        {
            string fileName = Path.GetFileName(sourcePath);
            int duplicates = 0;

            // Increment a duplicate counter until the file name does not exist
            while (FileExists(Path.Combine(destDirectory, fileName)))
            {
                if (Path.GetFileNameWithoutExtension(sourcePath) != "")
                {
                    fileName = string.Format("{0} ({1}){2}", Path.GetFileNameWithoutExtension(sourcePath),
                        ++duplicates, Path.GetExtension(sourcePath));
                }
                else fileName = string.Format("({0}){1}", ++duplicates, Path.GetExtension(sourcePath));
            }

            File.Copy(sourcePath, Path.Combine(destDirectory, fileName));
            renamed = duplicates != 0;
        }


        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public static int GWL_EXSTYLE = -20;
        public static int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr RegisterDeviceNotification(IntPtr recipient, IntPtr filter, int flags);

        [DllImport("user32.dll")]
        public static extern bool UnregisterDeviceNotification(IntPtr recipient);

        /// <summary>
        /// Represents information related to a WM_DEVICECHANGE message. This struct is actually a DEV_BROADCAST_HDR,
        /// but when <see cref="DeviceType"/> is 5 it takes on the <see cref="ClassGuid"/> and <see cref="Name"/> fields
        /// and becomes a DEV_BROADCAST_DEVICEINTERFACE.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DevBroadcastDeviceInterface
        {
            public int Size;
            public int DeviceType;
            public int Reserved;
            public Guid ClassGuid;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 255)]
            public string Name;

            /// <summary>
            /// Initialises a new instance of the <see cref="DevBroadcastDeviceInterface"/> struct.
            /// </summary>
            /// <param name="classGuid">The device interface class. Specifies the class of devices that will trigger
            /// the message.</param>
            public DevBroadcastDeviceInterface(Guid classGuid)
            {
                Size = 0;
                DeviceType = DBT_DEVTYP_DEVICEINTERFACE;
                Reserved = 0;
                ClassGuid = classGuid;
                Name = "";
            }
        }

        public static Guid GUID_DEVINTERFACE_VOLUME = new Guid("53F5630D-B6BF-11D0-94F2-00A0C91EFB8B");
        public static int DBT_DEVTYP_DEVICEINTERFACE = 5;

        public static int WM_DEVICE_CHANGE = 0x0219;
        public static int DBT_DEVICE_ARRIVAL = 0x8000;
        public static int DBT_DEVICE_REMOVE_COMPLETE = 0x8004;
    }
}
