using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace dcim_ingester
{
    public static class Helpers
    {
        public static List<string> GetVolumes()
        {
            List<string> volumes = new List<string>();
            ManagementObjectSearcher query = new ManagementObjectSearcher(
                "SELECT DeviceID FROM Win32_Volume WHERE DriveLetter != NULL");

            foreach (ManagementObject volume in query.Get())
            {
                // Get GUID only to avoid a mess with slashes and escaping
                string deviceId = volume["DeviceID"].ToString();
                deviceId = deviceId.Substring(deviceId.IndexOf('{'),
                    deviceId.IndexOf('}') - deviceId.IndexOf('{') + 1);
                volumes.Add(deviceId);
            }

            return volumes;
        }
        public static string GetVolumeLabel(string deviceId)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher(string.Format(
                "SELECT Label FROM Win32_Volume WHERE DeviceID LIKE '%{0}%'", deviceId));
            ManagementObjectCollection result = query.Get();

            if (result.Count > 0)
                return result.OfType<ManagementObject>().First()["Label"].ToString();
            return null;
        }
        public static string GetVolumeLetter(string deviceId)
        {
            ManagementObjectSearcher query = new ManagementObjectSearcher(string.Format(
                "SELECT DriveLetter FROM Win32_Volume WHERE DeviceID LIKE '%{0}%'", deviceId));
            ManagementObjectCollection result = query.Get();

            if (result.Count > 0)
                return result.OfType<ManagementObject>().First()["DriveLetter"].ToString();
            return null;
        }

        public static void PrintVolumes(List<string> volumes)
        {
            Console.WriteLine("VOLUMES:");

            foreach (string s in volumes)
                Console.WriteLine(GetVolumeLetter(s) + " -- " + s);
        }
    }
}
