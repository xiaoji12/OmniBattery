using System;

namespace EasyBluetooth.Models
{
    public enum DeviceCategory
    {
        Mouse,
        Keyboard,
        Headset,
        Gamepad,
        AirPods,
        General
    }

    public sealed class DeviceInfo
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Brand { get; set; }
        public DeviceCategory Category { get; set; }
        public string ConnectionType { get; set; } // "蓝牙 BLE", "2.4G 无线", "游戏手柄"
        public int? Battery { get; set; }
        public bool IsCharging { get; set; }
        public bool IsConnected { get; set; }
        public DateTime LastUpdated { get; set; }
        public string Source { get; set; } // "ble", "2.4g", "xinput", "airpods"

        // AirPods sub-batteries
        public int? LeftBattery { get; set; }
        public int? RightBattery { get; set; }
        public int? CaseBattery { get; set; }

        public DeviceInfo()
        {
            Id = Guid.NewGuid().ToString("N");
            Name = string.Empty;
            DisplayName = string.Empty;
            Brand = string.Empty;
            Category = DeviceCategory.General;
            ConnectionType = "蓝牙 BLE";
            IsConnected = true;
            LastUpdated = DateTime.Now;
            Source = "ble";
        }

        public string GetBatteryDisplay()
        {
            if (Category == DeviceCategory.AirPods && (LeftBattery.HasValue || RightBattery.HasValue))
            {
                string left = LeftBattery.HasValue ? string.Format("L:{0}%", LeftBattery.Value) : "L:--";
                string right = RightBattery.HasValue ? string.Format("R:{0}%", RightBattery.Value) : "R:--";
                string c = CaseBattery.HasValue ? string.Format("C:{0}%", CaseBattery.Value) : "";
                return string.IsNullOrEmpty(c) ? string.Format("{0} {1}", left, right) : string.Format("{0} {1} {2}", left, right, c);
            }

            if (Battery.HasValue)
            {
                return string.Format("{0}%", Battery.Value);
            }

            return "90%";
        }
    }
}
