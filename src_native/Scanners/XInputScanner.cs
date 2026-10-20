using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using EasyBluetooth.Models;

namespace EasyBluetooth.Scanners
{
    public static class XInputScanner
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct XINPUT_BATTERY_INFORMATION
        {
            public byte BatteryType;
            public byte BatteryLevel;
        }

        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetBatteryInformation")]
        private static extern int XInputGetBatteryInformation14(int dwUserIndex, byte devType, ref XINPUT_BATTERY_INFORMATION pBatteryInformation);

        [DllImport("xinput1_3.dll", EntryPoint = "XInputGetBatteryInformation")]
        private static extern int XInputGetBatteryInformation13(int dwUserIndex, byte devType, ref XINPUT_BATTERY_INFORMATION pBatteryInformation);

        private const byte BATTERY_DEVTYPE_GAMEPAD = 0x00;
        private const byte BATTERY_TYPE_DISCONNECTED = 0x00;
        private const byte BATTERY_TYPE_WIRED = 0x01;
        private const byte BATTERY_TYPE_ALKALINE = 0x02;
        private const byte BATTERY_TYPE_NIMH = 0x03;
        private const byte BATTERY_TYPE_UNKNOWN = 0xFF;

        public static List<DeviceInfo> Scan()
        {
            var results = new List<DeviceInfo>();

            for (int i = 0; i < 4; i++)
            {
                var info = new XINPUT_BATTERY_INFORMATION();
                int ret = -1;

                try
                {
                    ret = XInputGetBatteryInformation14(i, BATTERY_DEVTYPE_GAMEPAD, ref info);
                }
                catch
                {
                    try
                    {
                        ret = XInputGetBatteryInformation13(i, BATTERY_DEVTYPE_GAMEPAD, ref info);
                    }
                    catch
                    {
                        // XInput not available
                        break;
                    }
                }

                if (ret != 0 || info.BatteryType == BATTERY_TYPE_DISCONNECTED)
                {
                    // Disconnected -> DO NOT SHOW
                    continue;
                }

                int batteryPercent = 100;
                bool isCharging = false;

                if (info.BatteryType == BATTERY_TYPE_WIRED)
                {
                    batteryPercent = 100;
                    isCharging = true;
                }
                else
                {
                    switch (info.BatteryLevel)
                    {
                        case 0: batteryPercent = 10; break;
                        case 1: batteryPercent = 30; break;
                        case 2: batteryPercent = 70; break;
                        case 3: batteryPercent = 100; break;
                        default: batteryPercent = 80; break;
                    }
                }

                string name = string.Format("Xbox 无线手柄 #{0}", i + 1);
                results.Add(new DeviceInfo
                {
                    Id = "xinput_" + i,
                    Name = name,
                    DisplayName = name,
                    Brand = "Microsoft/微软",
                    Category = DeviceCategory.Gamepad,
                    ConnectionType = info.BatteryType == BATTERY_TYPE_WIRED ? "有线连接" : "Xbox 无线",
                    Battery = batteryPercent,
                    IsCharging = isCharging,
                    IsConnected = true,
                    LastUpdated = DateTime.Now,
                    Source = "xinput"
                });
            }

            return results;
        }
    }
}
