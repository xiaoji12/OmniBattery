using System;
using System.Collections.Generic;
using EasyBluetooth.Models;
using EasyBluetooth.Services;
using EasyBluetooth.Services.Providers;

namespace EasyBluetooth.Scanners
{
    public static class DeviceScanner
    {
        public static List<DeviceInfo> ScanAll()
        {
            var combined = new List<DeviceInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. Scan BLE and 2.4G PnP devices (Orochi V2, A98 BT, etc.)
            try
            {
                var pnpDevices = PnpScanner.Scan();
                foreach (var dev in pnpDevices)
                {
                    // Filter: Must be connected and have valid battery
                    if (!dev.IsConnected || !dev.Battery.HasValue)
                        continue;

                    string keyName = dev.Name.Trim().ToLowerInvariant();
                    string keyDisplay = dev.DisplayName.Trim().ToLowerInvariant();
                    if (!seen.Contains(keyName) && !seen.Contains(keyDisplay))
                    {
                        seen.Add(keyName);
                        seen.Add(keyDisplay);
                        combined.Add(dev);
                    }
                }
            }
            catch { }

            // 2. Scan active 2.4G wireless peripherals (AGM535R 2.4G, COLORFUL, G435, etc.)
            try
            {
                var hidDevices = HidScanner.Scan();
                foreach (var dev in hidDevices)
                {
                    if (!dev.IsConnected || !dev.Battery.HasValue)
                        continue;

                    string keyName = dev.Name.Trim().ToLowerInvariant();
                    string keyDisplay = dev.DisplayName.Trim().ToLowerInvariant();
                    if (!seen.Contains(keyName) && !seen.Contains(keyDisplay))
                    {
                        seen.Add(keyName);
                        seen.Add(keyDisplay);
                        combined.Add(dev);
                    }
                }
            }
            catch { }

            // 3. Scan XInput Game Controllers
            try
            {
                var gamepads = XInputScanner.Scan();
                foreach (var dev in gamepads)
                {
                    if (!dev.IsConnected)
                        continue;

                    string keyName = dev.Name.Trim().ToLowerInvariant();
                    string keyDisplay = dev.DisplayName.Trim().ToLowerInvariant();
                    if (!seen.Contains(keyName) && !seen.Contains(keyDisplay))
                    {
                        seen.Add(keyName);
                        seen.Add(keyDisplay);
                        combined.Add(dev);
                    }
                }
            }
            catch { }

            return combined;
        }
    }
}
