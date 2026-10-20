using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace EasyBluetooth.Services.Providers.Razer
{
    public sealed class RazerBatteryProvider : IBatteryProvider
    {
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
        private static extern bool HidD_GetFeature(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_SetFeature(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);

        public string Name { get { return "Razer / 雷蛇 2.4G Hyperspeed 协议提供者"; } }

        public bool CanHandle(string devicePath, string productName, string brand)
        {
            if (string.IsNullOrEmpty(devicePath)) return false;
            if (devicePath.IndexOf("VID_1532", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(brand) && brand.IndexOf("Razer", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(productName) && productName.IndexOf("Razer", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            return false;
        }

        public bool TryQueryBattery(string devicePath, out int batteryLevel, out bool isCharging)
        {
            batteryLevel = 0;
            isCharging = false;

            try
            {
                using (var handle = CreateFile(devicePath, 0xC0000000, 1 | 2, IntPtr.Zero, 3, 0, IntPtr.Zero))
                {
                    if (handle.IsInvalid) return false;

                    // Standard Razer Report 90 bytes (Report ID 0x00, Status 0x00, Transaction 0x1F, DataSize 0x02, CommandClass 0x07, CommandID 0x80)
                    byte[] report = new byte[90];
                    report[0] = 0x00;
                    report[1] = 0x00;
                    report[2] = 0x1F;
                    report[5] = 0x02; // size
                    report[6] = 0x07; // class
                    report[7] = 0x80; // command: get battery

                    // Compute checksum
                    byte crc = 0;
                    for (int i = 2; i < 88; i++) crc ^= report[i];
                    report[88] = crc;

                    if (HidD_SetFeature(handle, report, (uint)report.Length))
                    {
                        byte[] resp = new byte[90];
                        if (HidD_GetFeature(handle, resp, (uint)resp.Length))
                        {
                            int val = resp[9];
                            // Razer battery byte is 0-255 mapped to 0-100% or 0-100
                            if (val > 100) val = (int)Math.Round(val * 100.0 / 255.0);
                            if (val >= 1 && val <= 100)
                            {
                                batteryLevel = val;
                                isCharging = resp[10] == 1;
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

    public sealed class RazerPlugin : IDevicePlugin
    {
        public string Id { get { return "razer-2.4g-plugin"; } }
        public string Name { get { return "Razer / 雷蛇 2.4G VIP 扩展包"; } }
        public string Brand { get { return "Razer/雷蛇"; } }
        public string[] SupportedVids { get { return new string[] { "1532" }; } }
        public IBatteryProvider Provider { get; private set; }
        public bool IsEnabled { get; set; }

        public RazerPlugin()
        {
            Provider = new RazerBatteryProvider();
            IsEnabled = true;
        }
    }
}
