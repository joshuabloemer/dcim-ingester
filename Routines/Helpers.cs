using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace DCIMIngester.Routines
{
    public static class Helpers
    {
        public static string GetVolumeLetter(Guid volumeId)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher(string.Format(
                "SELECT DriveLetter FROM Win32_Volume WHERE DeviceID LIKE '%{0}%'", volumeId));
            ManagementObjectCollection result = query.Get();

            if (result.Count > 0)
                return result.OfType<ManagementObject>().First()["DriveLetter"].ToString();
            return null;
        }
        public static string GetVolumeLabel(Guid volumeId)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher(string.Format(
                "SELECT Label FROM Win32_Volume WHERE DeviceID LIKE '%{0}%'", volumeId));
            ManagementObjectCollection result = query.Get();

            if (result.Count > 0)
                return result.OfType<ManagementObject>().First()["Label"].ToString();
            return null;
        }

        // Taken from https://stackoverflow.com/questions/1242266/converting-bytes-to-gb-in-c
        public static string FormatBytes(long bytes)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB" };
            double dblSByte = bytes;

            int i;
            for (i = 0; i < suffix.Length && bytes >= 1024; i++, bytes /= 1024)
                dblSByte = bytes / 1024.0;

            return string.Format("{0:0.#} {1}", dblSByte, suffix[i]);
        }

        public static DateTime? GetTimeTaken(string filePath)
        {
            IEnumerable<MetadataExtractor.Directory> metaGroups
                = MetadataExtractor.ImageMetadataReader.ReadMetadata(filePath);
            MetadataExtractor.Tag metaTag = null;

            try
            {
                metaTag = metaGroups.First(group => group.Name == "Exif SubIFD")
                    .Tags.First(tag => tag.Name == "Date/Time Original");

                DateTime timeTaken = DateTime
                    .ParseExact(metaTag.Description, "yyyy:MM:dd HH:mm:ss", null);
                return timeTaken;
            }
            catch { return null; }
        }
    }
}
