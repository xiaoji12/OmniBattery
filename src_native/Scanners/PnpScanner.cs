using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using EasyBluetooth.Models;
using EasyBluetooth.Database;

namespace EasyBluetooth.Scanners
{
    public static class PnpScanner
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct DEVPROPKEY
        {
            public Guid fmtid;
            public uint pid;
        }

        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint CM_Get_Device_ID_List_SizeW(out uint pulLen, string pszFilter, uint ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint CM_Get_Device_ID_ListW(string pszFilter, char[] Buffer, uint BufferLen, uint ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint CM_Locate_DevNodeW(out uint pdnDevInst, string pDeviceID, uint ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        public static extern uint CM_Get_DevNode_Status(out uint pulStatus, out uint pulProblemNumber, uint dnDevInst, uint ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true)]
        public static extern uint CM_Get_DevNode_PropertyW(
            uint dnDevInst,
            ref DEVPROPKEY PropertyKey,
            out uint PropertyType,
            byte[] PropertyBuffer,
            ref uint PropertyBufferSize,
            uint ulFlags);

        private const uint DN_STARTED = 0x00000008;
        private const uint CM_GETIDLIST_FILTER_ENUMERATOR = 0x00000001;
        private const uint CM_GETIDLIST_FILTER_PRESENT = 0x00000100;

        private static readonly DEVPROPKEY KeyDeviceDesc = new DEVPROPKEY { fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), pid = 2 };
        private static readonly DEVPROPKEY KeyFriendlyName = new DEVPROPKEY { fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), pid = 14 };
        private static readonly DEVPROPKEY KeyBattery = new DEVPROPKEY { fmtid = new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"), pid = 2 };
        private static readonly DEVPROPKEY KeyBatteryStatus = new DEVPROPKEY { fmtid = new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"), pid = 3 };

        public static List<DeviceInfo> Scan()
        {
            var results = new List<DeviceInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                var activeAddrs = HidScanner.GetActiveInterfaceAddresses();

                // Scan BTHLE, HID
                ScanEnumerator("BTHLE", results, seen, activeAddrs);
                ScanEnumerator("HID", results, seen, activeAddrs);
            }
            catch { }

            return results;
        }

        private static void ScanEnumerator(string enumerator, List<DeviceInfo> results, HashSet<string> seen, HashSet<string> activeAddrs)
        {
            uint flags = CM_GETIDLIST_FILTER_ENUMERATOR | CM_GETIDLIST_FILTER_PRESENT;
            uint len;
            if (CM_Get_Device_ID_List_SizeW(out len, enumerator, flags) != 0 || len == 0)
                return;

            char[] buffer = new char[len];
            if (CM_Get_Device_ID_ListW(enumerator, buffer, len, flags) != 0)
                return;

            string all = new string(buffer);
            string[] deviceIds = all.Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string id in deviceIds)
            {
                // For Bluetooth LE devices, check if active interface is enabled
                if (enumerator.Equals("BTHLE", StringComparison.OrdinalIgnoreCase))
                {
                    string lowerId = id.ToLowerInvariant();
                    int devIdx = lowerId.IndexOf("dev_");
                    if (devIdx >= 0)
                    {
                        string addr = lowerId.Substring(devIdx + 4);
                        int slash = addr.IndexOf('\\');
                        if (slash > 0) addr = addr.Substring(0, slash);

                        if (!activeAddrs.Contains(addr))
                        {
                            // Disconnected Bluetooth device! Skip!
                            continue;
                        }
                    }
                }

                uint inst;
                if (CM_Locate_DevNodeW(out inst, id, 0) != 0)
                    continue;

                // Check status: MUST BE STARTED AND NO PROBLEM
                uint status, problem;
                if (CM_Get_DevNode_Status(out status, out problem, inst, 0) != 0)
                    continue;

                if ((status & DN_STARTED) == 0 || problem != 0)
                    continue;

                // Check battery property
                int? battery = GetByteProperty(inst, KeyBattery);
                if (!battery.HasValue)
                    continue;

                // Filter out invalid battery numbers
                if (battery.Value < 0 || battery.Value > 100)
                    continue;

                string rawName = GetStringProperty(inst, KeyFriendlyName) ?? GetStringProperty(inst, KeyDeviceDesc) ?? id;
                if (string.IsNullOrEmpty(rawName))
                    continue;

                // Deduplicate by name and battery
                string dedupeKey = rawName.ToLowerInvariant();
                if (seen.Contains(dedupeKey))
                    continue;
                seen.Add(dedupeKey);

                string brand, displayName;
                DeviceCategory cat;
                DeviceDatabase.Identify(rawName, out brand, out displayName, out cat);

                int? chargeStatus = GetByteProperty(inst, KeyBatteryStatus);
                bool isCharging = chargeStatus.HasValue && (chargeStatus.Value == 1 || chargeStatus.Value == 2);

                string connType = "蓝牙 BLE";
                if (id.IndexOf("USB", StringComparison.OrdinalIgnoreCase) >= 0 || id.IndexOf("HID\\VID", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    connType = "2.4G 无线";
                }

                results.Add(new DeviceInfo
                {
                    Id = id,
                    Name = rawName,
                    DisplayName = displayName,
                    Brand = brand,
                    Category = cat,
                    ConnectionType = connType,
                    Battery = battery.Value,
                    IsCharging = isCharging,
                    IsConnected = true,
                    LastUpdated = DateTime.Now,
                    Source = connType == "2.4G 无线" ? "2.4g" : "ble"
                });
            }
        }

        private static string GetStringProperty(uint inst, DEVPROPKEY key)
        {
            uint propType;
            uint size = 512;
            byte[] buf = new byte[size];
            if (CM_Get_DevNode_PropertyW(inst, ref key, out propType, buf, ref size, 0) == 0 && size > 2)
            {
                return Encoding.Unicode.GetString(buf, 0, (int)size - 2).TrimEnd('\0');
            }
            return null;
        }

        private static int? GetByteProperty(uint inst, DEVPROPKEY key)
        {
            uint propType;
            uint size = 16;
            byte[] buf = new byte[size];
            if (CM_Get_DevNode_PropertyW(inst, ref key, out propType, buf, ref size, 0) == 0 && size > 0)
            {
                return (int)buf[0];
            }
            return null;
        }
    }
}
