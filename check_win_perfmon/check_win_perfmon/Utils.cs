using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace check_win_perfmon
{
    internal class Utils
    {
        /// <summary>
        /// Get total memory of computer
        /// </summary>
        /// <param name="units">Units of memory</param>
        /// <returns>Memory in units</returns>
        public static ulong GetTotalMemory(string units)
        {
            //using Project -> Add Reference -> System.Management
            const string query = "SELECT Capacity FROM Win32_PhysicalMemory";
            var searcher = new ManagementObjectSearcher(query);

            ulong capacity = 0;
            foreach (var o in searcher.Get())
            {
                var wniPart = (ManagementObject) o;
                capacity += Convert.ToUInt64(wniPart.Properties["Capacity"].Value);
            }

            //Previous search failed. Trying alternative.
            if (capacity == 0)
            {
                //using Project -> Add Reference -> Visual Basic
                capacity = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory;
            }

            switch (units)
            {
                case "B":
                    break;
                case "KB":
                    capacity = capacity / 1024;
                    break;
                case "MB":
                    capacity = capacity / 1048576;
                    break;
                case "GB":
                    capacity = capacity / 1073741824;
                    break;
                case "TB":
                    capacity = capacity / 1099511627776;
                    break;
            }

            return capacity;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]

        private static extern int GetBestInterface(uint destAddr, out uint bestIfIndex);
        /// <summary>
        /// Auto detect internet connected network interface
        /// </summary>
        /// <returns>Internet connected network interface</returns>
        public static NetworkInterface GetNetworkInterface()
        {
            var ip = System.Net.IPAddress.Parse("8.8.8.8");
            GetBestInterface(BitConverter.ToUInt32(ip.GetAddressBytes(), 0), out var interfaceindex);
            // Search in all network interfaces that support IPv4.
            var ipv4Interface = (from thisInterface in NetworkInterface.GetAllNetworkInterfaces()
                where thisInterface.Supports(NetworkInterfaceComponent.IPv4)
                let ipv4Properties = thisInterface.GetIPProperties().GetIPv4Properties()
                where ipv4Properties != null && ipv4Properties.Index == interfaceindex
                select thisInterface).SingleOrDefault();
            if (ipv4Interface != null)
            {
                return ipv4Interface;
            }

            // Search in all network interfaces that support IPv6.
            var ipv6Interface = (from thisInterface in NetworkInterface.GetAllNetworkInterfaces()
                where thisInterface.Supports(NetworkInterfaceComponent.IPv6)
                let ipv6Properties = thisInterface.GetIPProperties().GetIPv6Properties()
                where ipv6Properties != null && ipv6Properties.Index == interfaceindex
                select thisInterface).SingleOrDefault();
            
            return ipv6Interface;
        }
        /// <summary>
        /// Get instance name of disk 0
        /// </summary>
        /// <returns>instance name of disk 0</returns>
        public static string GetDiskName()
        {
            var category = new PerformanceCounterCategory("PhysicalDisk");
            var instancename = category.GetInstanceNames();
            
            foreach (var name in instancename)
            {
                if (name.Contains("0 "))
                {
                    return name;
                }
            }
            return "_total";
        }
        /// <summary>
        /// Return interface speed
        /// </summary>
        /// <param name="name">name of interface</param>
        /// <returns></returns>
        public static float GetNetworkInterfaceSpeed(string name)
        {
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces().SingleOrDefault(x => x.Description == name);
            if (networkInterface != null)
            {
                return (float)networkInterface.Speed/8;
            }
            return 0;
        }
    }
}