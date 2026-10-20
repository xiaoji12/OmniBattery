using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
using EasyBluetooth.Models;
using EasyBluetooth.Database;
using EasyBluetooth.Services;
using EasyBluetooth.Services.Providers;

namespace EasyBluetooth.Scanners
{
    public static class HidScanner
    {
        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint CM_Get_Device_Interface_List_SizeW(
            out uint pulLen,
            ref Guid InterfaceClassGuid,
            string pDeviceID,
            uint ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint CM_Get_Device_Interface_ListW(
            ref Guid InterfaceClassGuid,
            string pDeviceID,
            char[] Buffer,
            uint BufferLen,
            uint ulFlags);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetProductString(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        public static extern bool HidD_GetManufacturerString(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        private static Guid GuidHid = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

        public static List<DeviceInfo> Scan()
        {
            var results = new List<DeviceInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            uint len;
            if (CM_Get_Device_Interface_List_SizeW(out len, ref GuidHid, null, 0) != 0 || len == 0)
                return results;

            char[] buf = new char[len];
            if (CM_Get_Device_Interface_ListW(ref GuidHid, null, buf, len, 0) != 0)
                return results;

            string[] paths = new string(buf).Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string path in paths)
            {
                // Filter virtual drivers, motherboard LED controllers
                if (path.IndexOf("VHF", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("LED", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("VIRTUAL", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    path.IndexOf("ROOT", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                // Identify 2.4G wireless receivers by VID/PID or name
                bool isKnown24GDongle =
                    path.IndexOf("VID_0BDA&PID_FFE7", StringComparison.OrdinalIgnoreCase) >= 0 || // ANTGAMER 2.4G
                    path.IndexOf("VID_260D", StringComparison.OrdinalIgnoreCase) >= 0 ||          // Dareu / 达尔优 2.4G
                    path.IndexOf("VID_046D", StringComparison.OrdinalIgnoreCase) >= 0 ||          // Logitech / 罗技 2.4G Lightspeed
                    path.IndexOf("VID_2F4C&PID_1000", StringComparison.OrdinalIgnoreCase) >= 0 || // COLORFUL / VGN 2.4G
                    path.IndexOf("VID_1532", StringComparison.OrdinalIgnoreCase) >= 0;            // Razer 2.4G HyperSpeed

                string prod = "";
                using (var handle = CreateFile(path, 0, 1 | 2, IntPtr.Zero, 3, 0, IntPtr.Zero))
                {
                    if (!handle.IsInvalid)
                    {
                        byte[] strBuf = new byte[256];
                        if (HidD_GetProductString(handle, strBuf, (uint)strBuf.Length))
                        {
                            prod = Encoding.Unicode.GetString(strBuf).TrimEnd('\0');
                        }
                    }
                }

                if (string.IsNullOrEmpty(prod))
                {
                    if (isKnown24GDongle)
                    {
                        prod = "2.4G 无线设备";
                    }
                    else
                    {
                        continue;
                    }
                }

                // Filter out non-peripherals / lighting / virtual drivers
                if (prod.IndexOf("LED", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prod.IndexOf("VHF", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prod.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    prod.IndexOf("Motherboard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    (prod.IndexOf("Control", StringComparison.OrdinalIgnoreCase) >= 0 && prod.IndexOf("Consumer", StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

                bool is24G = isKnown24GDongle ||
                            prod.IndexOf("2.4G", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            prod.IndexOf("Wireless", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            prod.IndexOf("Receiver", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            prod.IndexOf("Dongle", StringComparison.OrdinalIgnoreCase) >= 0;

                if (!is24G) continue;

                string brand, displayName;
                DeviceCategory cat;
                DeviceDatabase.Identify(prod, path, out brand, out displayName, out cat);

                int battery;
                bool isCharging;
                bool queryOk = PluginManager.TryQueryBattery(path, prod, displayName, brand, cat, out battery, out isCharging);

                if (!queryOk)
                {
                    // The 2.4G device is OFF, sleeping, or disconnected!
                    // Do NOT add it to active devices list!
                    continue;
                }

                // Deduplicate ONLY upon successful query so alternative endpoints are not shadowed
                string key = displayName.Trim().ToLowerInvariant();
                if (seen.Contains(key)) continue;
                seen.Add(key);

                results.Add(new DeviceInfo
                {
                    Id = path,
                    Name = prod,
                    DisplayName = displayName,
                    Brand = brand,
                    Category = cat,
                    ConnectionType = "2.4G 无线",
                    Battery = battery,
                    IsCharging = isCharging,
                    IsConnected = true,
                    LastUpdated = DateTime.Now,
                    Source = "2.4g"
                });
            }

            return results;
        }

        public static HashSet<string> GetActiveInterfaceAddresses()
        {
            var addrs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            uint len;
            if (CM_Get_Device_Interface_List_SizeW(out len, ref GuidHid, null, 0) != 0 || len == 0)
                return addrs;

            char[] buf = new char[len];
            if (CM_Get_Device_Interface_ListW(ref GuidHid, null, buf, len, 0) != 0)
                return addrs;

            string[] paths = new string(buf).Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string p in paths)
            {
                // Only inspect Bluetooth paths
                if (p.IndexOf("bth", StringComparison.OrdinalIgnoreCase) < 0 &&
                    p.IndexOf("00001812", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // Extract any 12-hex-character MAC address in the device path
                var matches = System.Text.RegularExpressions.Regex.Matches(p, @"(?i)(?:^|[^0-9a-f])([0-9a-f]{12})(?:[^0-9a-f]|$)");
                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    if (m.Groups.Count > 1 && m.Groups[1].Length == 12)
                    {
                        addrs.Add(m.Groups[1].Value.ToLowerInvariant());
                    }
                }
            }

            return addrs;
        }
    }
}
