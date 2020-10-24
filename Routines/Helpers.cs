using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using ExifSubIfdDirectory = MetadataExtractor.Formats.Exif.ExifSubIfdDirectory;

namespace DcimIngester.Routines
{
    internal static class Helpers
    {
        /// <summary>
        /// Gets the letter of a volume that is mounted to the system.
        /// </summary>
        /// <param name="volumeId">The GUID of the volume.</param>
        /// <returns>The volume letter followed by a colon, or <see langword="null"/> if the volume was not found</returns>
        public static string GetVolumeLetter(Guid volumeId)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher(string.Format(
                "SELECT DriveLetter FROM Win32_Volume WHERE DeviceID LIKE '%{0}%'", volumeId));
            ManagementObjectCollection result = query.Get();

            if (result.Count > 0)
                return result.OfType<ManagementObject>().First()["DriveLetter"].ToString();
            return null;
        }

        /// <summary>
        /// Gets the label of a volume that is mounted to the system.
        /// </summary>
        /// <param name="volumeId">The GUID of the volume.</param>
        /// <returns>The volume label, an empty string if the volume has no label, or <see langword="null"/> if the volume was not found.</returns>
        public static string GetVolumeLabel(Guid volumeId)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher(string.Format(
                "SELECT Label FROM Win32_Volume WHERE DeviceID LIKE '%{0}%'", volumeId));
            ManagementObjectCollection result = query.Get();

            if (result.Count > 0)
            {
                object label = result.OfType<ManagementObject>().First()["Label"];
                return label == null ? "" : label.ToString();
            }

            return null;
        }

        /// <summary>
        /// Formats a numerical storage size into a string with the relevant units.
        /// </summary>
        /// <param name="bytes">The storage size in bytes.</param>
        /// <returns>The formatted storage size.</returns>
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
        /// Gets the "Date/Time Original" attribute from the EXIF data of a file.
        /// </summary>
        /// <param name="path">The file to get the attribute for.</param>
        /// <returns>The "Date/Time Original" attribute value, or <see langword="null"/> if the attribute does not exist.</returns>
        public static DateTime? GetDateTaken(string path)
        {
            IEnumerable<MetadataExtractor.Directory> metadata;
            try
            {
                metadata = MetadataExtractor.ImageMetadataReader.ReadMetadata(path);
            }
            catch (MetadataExtractor.ImageProcessingException) { return null; }

            string dateTime;
            try
            {
                ExifSubIfdDirectory exifSubIfd = metadata.OfType<ExifSubIfdDirectory>().Single();
                dateTime = exifSubIfd.Tags.Single(tag => tag.Name == "Date/Time Original").Description;
            }
            catch (InvalidOperationException) { return null; }

            try
            {
                return DateTime.ParseExact(dateTime, "yyyy:MM:dd HH:mm:ss", null);
            }
            catch (FormatException) { return null; }
        }

        /// <summary>
        /// Checks if a directory exists. Differs from <see cref="Directory.Exists(string)"/> by throwing an exception if there is an error.
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
        /// Checks if a file exists. Differs from <see cref="File.Exists(string)"/> by throwing an exception if there is an error.
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
        /// Creates a directory if it does not exist. If the directory exists but has extra text appended to the directory name, it is not created.
        /// </summary>
        /// <param name="path">The directory to create. This should contain at least one non-root path section.</param>
        /// <returns>The created or already existing directory.</returns>
        public static string CreateIngestDirectory(string path)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(path);

            if (DirectoryExists(dirInfo.Parent.FullName))
            {
                // Check if any directory names starting with the final directory name exist .This
                // allows users to have e.g. a description at the end of the directory name
                string[] directories = Directory.GetDirectories(dirInfo.Parent.FullName, dirInfo.Name + "*");

                if (directories.Length > 0)
                    return Path.Combine(dirInfo.Parent.FullName, new DirectoryInfo(directories[0]).Name);
            }

            return Directory.CreateDirectory(path).FullName;
        }

        /// <summary>
        /// Copies a file to a directory. If the file already exists in the directory then a counter is added to the file name.
        /// </summary>
        /// <param name="sourcePath">The file to copy.</param>
        /// <param name="destDirectory">The directory to copy the file to.</param>
        /// <param name="isRenamed">Indicates whether the destination file name was changed to avoid duplication.</param>
        public static void CopyFile(string sourcePath, string destDirectory, out bool isRenamed)
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
            isRenamed = duplicates != 0;
        }


        [DllImport("user32.dll", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TOOLWINDOW = 0x00000080;
    }
}
