using System;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace EasyBluetooth.Services.Providers.Antgamer
{
    public sealed class AntgamerBatteryProvider : IBatteryProvider
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
        private static extern bool HidD_SetOutputReport(SafeFileHandle HidDeviceObject, byte[] Buffer, uint BufferLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadFile(SafeFileHandle hFile, byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, [In] ref NativeOverlapped lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetOverlappedResult(SafeFileHandle hFile, [In] ref NativeOverlapped lpOverlapped, out uint lpNumberOfBytesTransferred, bool bWait);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private static Guid GuidHid = new Guid("4D1E55B2-F16F-11CF-88CB-001111000030");

        public string Name { get { return "ANTGAMER / 蚂蚁电竞 2.4G 协议提供者"; } }

        public bool CanHandle(string devicePath, string productName, string brand)
        {
            if (string.IsNullOrEmpty(devicePath)) return false;
            if (devicePath.IndexOf("VID_0BDA&PID_FFE7", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(brand) && brand.IndexOf("蚂蚁电竞", StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (!string.IsNullOrEmpty(productName) && productName.IndexOf("AGM535R", StringComparison.OrdinalIgnoreCase) >= 0) return true;
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

                foreach (var p in paths)
                {
                    if (p.IndexOf("0BDA", StringComparison.OrdinalIgnoreCase) < 0 ||
                        p.IndexOf("FFE7", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    // Query communication endpoint MI_04 or MI_02
                    if (p.IndexOf("MI_04", StringComparison.OrdinalIgnoreCase) < 0 &&
                        p.IndexOf("MI_02", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    using (var handle = CreateFile(p, 0xC0000000, 1 | 2 | 4, IntPtr.Zero, 3, 0x40000000 /* FILE_FLAG_OVERLAPPED */, IntPtr.Zero))
                    {
                        if (handle.IsInvalid) continue;

                        // 1:1 match with original EasyBluetooth e.z: 64 bytes command [179, 6, 0...]
                        byte[] req = new byte[64];
                        req[0] = 179; // 0xB3
                        req[1] = 6;   // 0x06

                        if (!HidD_SetOutputReport(handle, req, (uint)req.Length))
                            continue;

                        IntPtr hEvent = CreateEvent(IntPtr.Zero, true, false, null);
                        var overlapped = new NativeOverlapped { EventHandle = hEvent };

                        byte[] resp = new byte[64];
                        uint bytesRead;
                        bool readSuccess = ReadFile(handle, resp, (uint)resp.Length, out bytesRead, ref overlapped);
                        int err = Marshal.GetLastWin32Error();

                        if (!readSuccess && err == 997) // ERROR_IO_PENDING
                        {
                            uint wait = WaitForSingleObject(hEvent, 800);
                            if (wait == 0)
                            {
                                GetOverlappedResult(handle, ref overlapped, out bytesRead, false);
                                readSuccess = true;
                            }
                        }

                        CloseHandle(hEvent);

                        if (readSuccess && bytesRead >= 21)
                        {
                            // 1:1 match with original EasyBluetooth e.z: response signature [180, 6...]
                            if (resp[0] == 180 && resp[1] == 6)
                            {
                                int val = resp[20] & 0x7F;
                                if (val >= 0 && val <= 100)
                                {
                                    batteryLevel = val;
                                    isCharging = (resp[20] & 0x80) != 0;
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return false;
        }
    }

    public sealed class AntgamerPlugin : IDevicePlugin
    {
        public string Id { get { return "antgamer-2.4g-plugin"; } }
        public string Name { get { return "ANTGAMER / 蚂蚁电竞 2.4G VIP 扩展包"; } }
        public string Brand { get { return "ANTGAMER/蚂蚁电竞"; } }
        public string[] SupportedVids { get { return new string[] { "0BDA" }; } }
        public IBatteryProvider Provider { get; private set; }
        public bool IsEnabled { get; set; }

        public AntgamerPlugin()
        {
            Provider = new AntgamerBatteryProvider();
            IsEnabled = true;
        }
    }
}
