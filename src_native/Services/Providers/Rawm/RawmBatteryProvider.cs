using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace EasyBluetooth.Services.Providers.Rawm
{
    public struct RawmQueryPlan
    {
        public byte ReportId;
        public byte OpCode;
        public int BatteryOffset;

        public RawmQueryPlan(byte repId, byte opCode, int batOffset)
        {
            ReportId = repId;
            OpCode = opCode;
            BatteryOffset = batOffset;
        }
    }

    public sealed class RawmBatteryProvider : IBatteryProvider
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

        private static readonly RawmQueryPlan[] QueryPlans = new RawmQueryPlan[]
        {
            new RawmQueryPlan(0x02, 0x91, 2),
            new RawmQueryPlan(0x07, 0x01, 1),
            new RawmQueryPlan(0x08, 0x03, 3)
        };

        public string Name { get { return "RAWM / 仁魔 2.4G 协议提供者"; } }

        public bool CanHandle(string devicePath, string productName, string brand)
        {
            if (string.IsNullOrEmpty(devicePath)) return false;
            string lower = (productName + " " + brand + " " + devicePath).ToLowerInvariant();
            return lower.IndexOf("rawm") >= 0 || lower.IndexOf("仁魔") >= 0;
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

                    foreach (var plan in QueryPlans)
                    {
                        byte[] req = new byte[65];
                        req[0] = plan.ReportId;
                        req[1] = plan.OpCode;

                        HidD_SetFeature(handle, req, (uint)req.Length);

                        byte[] resp = new byte[65];
                        resp[0] = plan.ReportId;
                        if (HidD_GetFeature(handle, resp, (uint)resp.Length) || HidD_GetInputReport(handle, resp, (uint)resp.Length))
                        {
                            int b = resp[plan.BatteryOffset];
                            if (b >= 5 && b <= 100)
                            {
                                batteryLevel = b;
                                isCharging = resp[plan.BatteryOffset + 1] == 1;
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

    public sealed class RawmPlugin : IDevicePlugin
    {
        public string Id { get { return "rawm-2.4g-plugin"; } }
        public string Name { get { return "RAWM / 仁魔 2.4G VIP 扩展包"; } }
        public string Brand { get { return "RAWM/仁魔"; } }
        public string[] SupportedVids { get { return new string[] { "258A", "0C40" }; } }
        public IBatteryProvider Provider { get; private set; }
        public bool IsEnabled { get; set; }

        public RawmPlugin()
        {
            Provider = new RawmBatteryProvider();
            IsEnabled = true;
        }
    }
}
