using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text.RegularExpressions;

namespace dcim_ingester.Routines
{
    public static class Helpers
    {
        public enum TaskStatus { Waiting, Transferring, Completed, Failed };

        public static List<Guid> GetVolumes()
        {
            List<Guid> volumes = new List<Guid>();
            ManagementObjectSearcher query = new ManagementObjectSearcher(
                "SELECT DeviceID FROM Win32_Volume WHERE DriveLetter != NULL "
                + "AND (FileSystem = 'FAT12' OR FileSystem = 'FAT16' OR "
                + "FileSystem = 'FAT32' OR FileSystem = 'exFAT')");

            foreach (ManagementObject volume in query.Get())
            {
                // Extract GUID to avoid a mess with slashes and escaping
                string volumeId = volume["DeviceID"].ToString();
                volumeId = volumeId.Substring(volumeId.IndexOf('{'),
                    volumeId.IndexOf('}') - volumeId.IndexOf('{') + 1);
                volumes.Add(new Guid(volumeId));
            }

            return volumes;
        }
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

        public static void PrintVolumes(List<Guid> volumes)
        {
            Console.WriteLine("VOLUMES:");

            foreach (Guid volume in volumes)
                Console.WriteLine(GetVolumeLetter(volume) + " -- " + volume);
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

        public static bool IsFilesToTransfer(string volumePath)
        {
            try
            {
                string[] directories = Directory
                    .GetDirectories(Path.Combine(volumePath, "DCIM"));
                if (directories.Length == 0) return false;

                foreach (string directory in directories)
                {
                    // Ignore directory names not conforming to DCF spec
                    if (!Regex.IsMatch(Path.GetFileName(
                        directory), "^[0-9]{3}[0-9a-zA-Z]{5}$")) continue;

                    string[] files = Directory.GetFiles(directory);

                    foreach (string file in files)
                    {
                        // Ignore file names not conforming to DCF spec
                        if (!Regex.IsMatch(Path.GetFileNameWithoutExtension(
                            file), "^[0-9a-zA-Z_]{4}[0-9]{4}$")) continue;
                        return true;
                    }
                }

                return false;
            }
            catch (Exception) { return false; }
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
            catch (Exception) { return null; }
        }
    }
}
