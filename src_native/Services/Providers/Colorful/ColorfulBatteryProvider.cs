using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace EasyBluetooth.Services.Providers.Colorful
{
    public sealed class ColorfulBatteryProvider : IBatteryProvider
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
        private static extern bool HidD_GetInputReport(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_GetFeature(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);

        public string Name { get { return "COLORFUL / 七彩虹 2.4G 协议提供者"; } }

        public bool CanHandle(string devicePath, string productName, string brand)
        {
            if (string.IsNullOrEmpty(devicePath)) return false;
            if (devicePath.IndexOf("VID_2F4C&PID_1000", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(brand) && brand.IndexOf("COLORFUL", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(productName) && productName.IndexOf("COLORFUL", StringComparison.OrdinalIgnoreCase) >= 0) return true;
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

                    // COLORFUL vendor protocol report
                    byte[] inRep = new byte[65];
                    inRep[0] = 0x00;
                    if (HidD_GetInputReport(handle, inRep, (uint)inRep.Length))
                    {
                        if (inRep[1] >= 5 && inRep[1] <= 100)
                        {
                            batteryLevel = inRep[1];
                            isCharging = inRep[2] == 1;
                            return true;
                        }
                    }

                    byte[] feat = new byte[65];
                    feat[0] = 0x01;
                    if (HidD_GetFeature(handle, feat, (uint)feat.Length))
                    {
                        if (feat[1] >= 5 && feat[1] <= 100)
                        {
                            batteryLevel = feat[1];
                            isCharging = feat[2] == 1;
                            return true;
                        }
                    }
                }
            }
            catch { }

            return false;
        }
    }

    public sealed class ColorfulPlugin : IDevicePlugin
    {
        public string Id { get { return "colorful-2.4g-plugin"; } }
        public string Name { get { return "COLORFUL / 七彩虹 2.4G VIP 扩展包"; } }
        public string Brand { get { return "COLORFUL/七彩虹"; } }
        public string[] SupportedVids { get { return new string[] { "2F4C" }; } }
        public IBatteryProvider Provider { get; private set; }
        public bool IsEnabled { get; set; }

        public ColorfulPlugin()
        {
            Provider = new ColorfulBatteryProvider();
            IsEnabled = true;
        }
    }
}
