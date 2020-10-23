using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using ExifSubIfdDirectory = MetadataExtractor.Formats.Exif.ExifSubIfdDirectory;

namespace DCIMIngester.Routines
{
    internal static class Helpers
    {
        internal static string GetVolumeLetter(Guid volumeId)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher(string.Format(
                "SELECT DriveLetter FROM Win32_Volume WHERE DeviceID LIKE '%{0}%'", volumeId));
            ManagementObjectCollection result = query.Get();

            if (result.Count > 0)
                return result.OfType<ManagementObject>().First()["DriveLetter"].ToString();
            return null;
        }
        internal static string GetVolumeLabel(Guid volumeId)
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

        // From https://stackoverflow.com/questions/1242266/converting-bytes-to-gb-in-c
        internal static string FormatBytes(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            double dblSByte = bytes;

            int i;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
                dblSByte = bytes / 1024.0;

            return string.Format("{0:0.#} {1}", dblSByte, suffix[i]);
        }

        internal static DateTime? GetTimeTaken(string filePath)
        {
            IEnumerable<MetadataExtractor.Directory> metadataGroups;
            try
            {
                metadataGroups = MetadataExtractor.ImageMetadataReader.ReadMetadata(filePath);
            }
            catch (MetadataExtractor.ImageProcessingException) { return null; }

            // Search for the field containing the time that the image was taken
            ExifSubIfdDirectory subIfdGroup = metadataGroups.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (subIfdGroup == null) return null;

            MetadataExtractor.Tag dateTimeOriginal =
                subIfdGroup.Tags.FirstOrDefault(tag => tag.Name == "Date/Time Original");
            if (dateTimeOriginal == null) return null;

            try
            {
                DateTime timeTaken = DateTime.ParseExact(dateTimeOriginal.Description, "yyyy:MM:dd HH:mm:ss", null);
                return timeTaken;
            }
            catch { return null; }
        }

        internal static bool DirectoryExists(string directory)
        {
            // GetCreationTime() will throw an exception if there is any error, otherwise
            // it will return the below constant time if the directory does not exist
            DateTime creationTime = Directory.GetCreationTime(directory);
            return creationTime == new DateTime(1601, 1, 1, 0, 0, 0) ? false : true;
        }
        internal static string CreateDirectory(string directory)
        {
            string lastSegment = Path.GetFileName(directory);
            int lastSegmentPosition = directory.IndexOf(lastSegment);
            string withoutLastSegment = directory.Remove(lastSegmentPosition, lastSegment.Length);

            // First ensure the directory tree excluding the final directory name exists
            if (!DirectoryExists(withoutLastSegment))
            {
                Directory.CreateDirectory(directory);
                return directory;
            }

            // Then check if any directory names starting with the final directory name exist
            // (this allows adding e.g. a description to the end of an image directory name).
            // Also, this method allows us to distinguish a directory not existing from an
            // error determining whether it exists
            string[] directories = Directory.GetDirectories(withoutLastSegment, lastSegment + "*");

            if (directories.Length > 0)
                return Path.Combine(withoutLastSegment, Path.GetFileName(directories[0]));

            // Directory doesn't exist so create it
            Directory.CreateDirectory(directory);
            return directory;
        }
        internal static bool CopyFile(string sourceFilePath, string destinationDir)
        {
            string fileName = Path.GetFileName(sourceFilePath);
            string newFileName = fileName;

            // This method allows us to distinguish a file not existing from an error
            // determining whether it exists
            string[] files = Directory.GetFiles(destinationDir, Path.GetFileNameWithoutExtension(fileName) + "*");

            int duplicateCounter = 0;

            // Increment a duplicate counter until the file name does not exist
            while (files.Contains(Path.Combine(destinationDir, newFileName)))
            {
                newFileName = string.Format("{0} ({1}){2}", Path.GetFileNameWithoutExtension(fileName),
                    duplicateCounter + 1, Path.GetExtension(fileName));
                duplicateCounter++;
            }

            File.Copy(sourceFilePath, Path.Combine(destinationDir, newFileName));
            return duplicateCounter == 0 ? false : true;
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        internal const int GWL_EXSTYLE = -20;
        internal const int WS_EX_TOOLWINDOW = 0x00000080;
    }
}
