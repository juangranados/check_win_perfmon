using System;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

internal class Utils
{
    /// <summary>
    /// Get total memory of computer
    /// </summary>
    /// <param name="units">Units of memory</param>
    /// <returns>Memory in units</returns>
    static public ulong GetTotalMemory(string units)
    {
        //using Project -> Add Reference -> System.Management
        string Query = "SELECT Capacity FROM Win32_PhysicalMemory";
        ManagementObjectSearcher searcher = new ManagementObjectSearcher(Query);

        UInt64 Capacity = 0;
        foreach (ManagementObject WniPART in searcher.Get())
        {
            Capacity += Convert.ToUInt64(WniPART.Properties["Capacity"].Value);
        }
        switch (units)
        {
            case "B":
                break;
            case "KB":
                Capacity = Capacity / 1024;
                break;
            case "MB":
                Capacity = Capacity / 1048576;
                break;
            case "GB":
                Capacity = Capacity / 1073741824;
                break;
            case "TB":
                Capacity = Capacity / 1099511627776;
                break;
        }
        return Capacity;
    }
    [DllImport("iphlpapi.dll", SetLastError = true)]
    /// <summary>
    /// Get the best interface for reaching IP address
    /// </summary>
    /// <param name="DestAddr">Destination address to reach</param>
    /// <param name="BestIfIndex">Best network interface to get DestAddr interface</param>
    /// <returns>Best network interface to get DestAddr interface</returns>
    static extern int GetBestInterface(UInt32 DestAddr, out UInt32 BestIfIndex);
    /// <summary>
    /// Auto detect internet connected network interface
    /// </summary>
    /// <returns>Internet connected network interface</returns>
    static public NetworkInterface GetNetworkInterface()
    {
        System.Net.IPAddress ip = System.Net.IPAddress.Parse("8.8.8.8");
        UInt32 interfaceindex;
        GetBestInterface(BitConverter.ToUInt32(ip.GetAddressBytes(), 0), out interfaceindex);
        // Search in all network interfaces that support IPv4.
        NetworkInterface ipv4Interface = (from thisInterface in NetworkInterface.GetAllNetworkInterfaces()
                                          where thisInterface.Supports(NetworkInterfaceComponent.IPv4)
                                          let ipv4Properties = thisInterface.GetIPProperties().GetIPv4Properties()
                                          where ipv4Properties != null && ipv4Properties.Index == interfaceindex
                                          select thisInterface).SingleOrDefault();
        if (ipv4Interface != null)
        {
            return ipv4Interface;
        }

        // Search in all network interfaces that support IPv6.
        NetworkInterface ipv6Interface = (from thisInterface in NetworkInterface.GetAllNetworkInterfaces()
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
    static public string GetDiskName()
    {
        PerformanceCounterCategory category = new PerformanceCounterCategory("PhysicalDisk");
        String[] instancename = category.GetInstanceNames();
        //Console.WriteLine(category.GetCounters());
        foreach (string name in instancename)
        {
            if (name.Contains("0 "))
            {
                return name;
            }
        }
        return "_total";
    }
}