using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace EasyBluetooth.Services.Providers.Pmo
{
    public struct PmoCommandPair
    {
        public byte ReportId;
        public byte CommandId;
        public byte ResponseReportId;
        public int BatteryByteOffset;
        public int ChargingByteOffset;

        public PmoCommandPair(byte repId, byte cmdId, byte respRepId, int batOffset, int chgOffset)
        {
            ReportId = repId;
            CommandId = cmdId;
            ResponseReportId = respRepId;
            BatteryByteOffset = batOffset;
            ChargingByteOffset = chgOffset;
        }
    }

    public sealed class PmoBatteryProvider : IBatteryProvider
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

        [DllImport("hid.dll", SetLastError = true)]
        private static extern bool HidD_SetFeature(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);

        private static readonly PmoCommandPair[] StandardCommandPairs = new PmoCommandPair[]
        {
            new PmoCommandPair(0x07, 0x02, 0x07, 1, 2),
            new PmoCommandPair(0x08, 0x02, 0x08, 3, 4),
            new PmoCommandPair(0x04, 0x01, 0x04, 2, 3)
        };

        public string Name { get { return "PMO / PAW3395 公版无线协议提供者"; } }

        public bool CanHandle(string devicePath, string productName, string brand)
        {
            if (string.IsNullOrEmpty(devicePath)) return false;
            string lower = (productName + " " + brand + " " + devicePath).ToLowerInvariant();
            return lower.IndexOf("vgn") >= 0 ||
                   lower.IndexOf("atk") >= 0 ||
                   lower.IndexOf("mchose") >= 0 ||
                   lower.IndexOf("darmoshark") >= 0 ||
                   lower.IndexOf("pmo") >= 0 ||
                   lower.IndexOf("wlmouse") >= 0 ||
                   lower.IndexOf("incott") >= 0;
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

                    foreach (var pair in StandardCommandPairs)
                    {
                        byte[] req = new byte[65];
                        req[0] = pair.ReportId;
                        req[1] = pair.CommandId;

                        // Try SetFeature then GetFeature
                        HidD_SetFeature(handle, req, (uint)req.Length);

                        byte[] resp = new byte[65];
                        resp[0] = pair.ResponseReportId;
                        if (HidD_GetFeature(handle, resp, (uint)resp.Length) || HidD_GetInputReport(handle, resp, (uint)resp.Length))
                        {
                            int b = resp[pair.BatteryByteOffset];
                            if (b >= 5 && b <= 100)
                            {
                                batteryLevel = b;
                                isCharging = resp[pair.ChargingByteOffset] == 1;
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

    public sealed class PmoPlugin : IDevicePlugin
    {
        public string Id { get { return "pmo-2.4g-plugin"; } }
        public string Name { get { return "PMO / VGN / ATK / 迈从 2.4G VIP 扩展包"; } }
        public string Brand { get { return "VGN/ATK/迈从"; } }
        public string[] SupportedVids { get { return new string[] { "258A", "3554", "3434" }; } }
        public IBatteryProvider Provider { get; private set; }
        public bool IsEnabled { get; set; }

        public PmoPlugin()
        {
            Provider = new PmoBatteryProvider();
            IsEnabled = true;
        }
    }
}
