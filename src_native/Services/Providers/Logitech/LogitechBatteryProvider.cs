using System;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace EasyBluetooth.Services.Providers.Logitech
{
    public sealed class LogitechBatteryProvider : IBatteryProvider
    {
        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint CM_Get_Device_Interface_List_SizeW(out uint pulLen, ref Guid InterfaceClassGuid, string pDeviceID, uint ulFlags);

        [DllImport("cfgmgr32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern uint CM_Get_Device_Interface_ListW(ref Guid InterfaceClassGuid, string pDeviceID, char[] Buffer, uint BufferLen, uint ulFlags);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetInputReport(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetFeature(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_SetOutputReport(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteFile(SafeFileHandle hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

        private static Guid GuidHid = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

        public string Name { get { return "Logitech / 罗技 2.4G Lightspeed 协议提供者"; } }

        public bool CanHandle(string devicePath, string productName, string brand)
        {
            if (string.IsNullOrEmpty(devicePath)) return false;
            if (devicePath.IndexOf("VID_046D", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(brand) && brand.IndexOf("Logitech", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(productName) && (productName.IndexOf("G435", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                      productName.IndexOf("Logitech", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                      productName.IndexOf("G Pro", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                      productName.IndexOf("GPW", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                      productName.IndexOf("G304", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                      productName.IndexOf("G733", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                                      productName.IndexOf("G502", StringComparison.OrdinalIgnoreCase) >= 0)) return true;
            return false;
        }

        public bool TryQueryBattery(string devicePath, out int batteryLevel, out bool isCharging)
        {
            batteryLevel = -1;
            isCharging = false;

            try
            {
                uint len;
                if (CM_Get_Device_Interface_List_SizeW(out len, ref GuidHid, null, 0) != 0 || len == 0)
                    return false;

                char[] buf = new char[len];
                if (CM_Get_Device_Interface_ListW(ref GuidHid, null, buf, len, 0) != 0)
                    return false;

                string[] paths = new string(buf).Split(new char[] { '\0' }, StringSplitOptions.RemoveEmptyEntries);

                // 1. Logitech G435 Wireless Gaming Headset (046D:0ADF)
                if (devicePath.IndexOf("0ADF", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    devicePath.IndexOf("G435", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    foreach (var p in paths)
                    {
                        if (p.IndexOf("046D", StringComparison.OrdinalIgnoreCase) < 0 ||
                            p.IndexOf("0ADF", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        // Target communication endpoint MI_00
                        if (p.IndexOf("MI_00", StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        using (var handle = CreateFile(p, 0xC0000000, 1 | 2, IntPtr.Zero, 3, 0, IntPtr.Zero))
                        {
                            if (handle.IsInvalid) continue;

                            byte[] outBuf = new byte[65];
                            outBuf[0] = 0x11;
                            outBuf[1] = 0xFF;
                            outBuf[2] = 0x08;
                            outBuf[3] = 0x0B;

                            uint written;
                            WriteFile(handle, outBuf, 65, out written, IntPtr.Zero);

                            byte[] inBuf = new byte[65];
                            inBuf[0] = 0x11;
                            if (HidD_GetInputReport(handle, inBuf, (uint)inBuf.Length))
                            {
                                int bat = inBuf[1];
                                if (bat > 0 && bat <= 100)
                                {
                                    batteryLevel = bat;
                                    isCharging = (inBuf[2] == 1 || inBuf[2] == 2);
                                    return true;
                                }
                            }
                        }
                    }

                    // Headset is off / disconnected
                    return false;
                }

                // 2. Standard Logitech HID++ 1.0 / 2.0 (GPW, G304, G502, G913, etc.)
                foreach (var p in paths)
                {
                    if (p.IndexOf("046D", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    using (var handle = CreateFile(p, 0xC0000000, 1 | 2, IntPtr.Zero, 3, 0, IntPtr.Zero))
                    {
                        if (handle.IsInvalid) continue;

                        byte[] inBuf = new byte[65];
                        inBuf[0] = 0;
                        if (HidD_GetInputReport(handle, inBuf, (uint)inBuf.Length))
                        {
                            if (inBuf[1] >= 1 && inBuf[1] <= 100)
                            {
                                batteryLevel = inBuf[1];
                                isCharging = (inBuf[2] == 1);
                                return true;
                            }
                        }

                        // Try HID++ 1.0 Battery Status request [0x10, 0xFF, 0x09, 0, 0, 0, 0]
                        byte[] shortRep = new byte[7] { 0x10, 0xFF, 0x09, 0, 0, 0, 0 };
                        if (HidD_GetFeature(handle, shortRep, (uint)shortRep.Length))
                        {
                            if (shortRep[3] >= 0 && shortRep[3] <= 100)
                            {
                                batteryLevel = shortRep[3];
                                isCharging = (shortRep[4] == 1);
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }

            return false;
        }
    }

    public sealed class LogitechPlugin : IDevicePlugin
    {
        public string Id { get { return "logitech-2.4g-plugin"; } }
        public string Name { get { return "Logitech / 罗技 2.4G VIP 扩展包"; } }
        public string Brand { get { return "Logitech/罗技"; } }
        public string[] SupportedVids { get { return new string[] { "046D" }; } }
        public IBatteryProvider Provider { get; private set; }
        public bool IsEnabled { get; set; }

        public LogitechPlugin()
        {
            Provider = new LogitechBatteryProvider();
            IsEnabled = true;
        }
    }
}
