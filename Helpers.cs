using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

namespace dcim_ingester
{
    public static class Helpers
    {
        public static List<Guid> GetVolumes()
        {
            List<Guid> volumes = new List<Guid>();
            ManagementObjectSearcher query = new ManagementObjectSearcher(
                "SELECT DeviceID FROM Win32_Volume WHERE DriveLetter != NULL");

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
    }
}
