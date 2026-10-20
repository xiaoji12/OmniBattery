using System;
using System.Collections.Generic;
using Microsoft.Win32;
using EasyBluetooth.Models;

namespace EasyBluetooth.Services
{
    public static class Aida64Exporter
    {
        private const string RegistryPath = @"Software\FinalWire\AIDA64\ImportValues";
        private const int MaxSlots = 10;

        public static void Export(IList<DeviceInfo> devices)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegistryPath, RegistryKeyPermissionCheck.ReadWriteSubTree))
                {
                    if (key == null) return;

                    int count = Math.Min(devices.Count, MaxSlots);
                    for (int i = 0; i < count; i++)
                    {
                        var dev = devices[i];
                        int slot = i + 1;
                        string text = string.Format("{0}: {1}", dev.DisplayName, dev.GetBatteryDisplay());
                        if (dev.IsCharging) text += " (充电中)";

                        key.SetValue("Str" + slot, text, RegistryValueKind.String);
                        key.SetValue("DW" + slot, dev.Battery.HasValue ? dev.Battery.Value : 0, RegistryValueKind.DWord);
                    }

                    // Clear remaining slots
                    for (int i = count; i < MaxSlots; i++)
                    {
                        int slot = i + 1;
                        key.SetValue("Str" + slot, string.Empty, RegistryValueKind.String);
                        key.SetValue("DW" + slot, 0, RegistryValueKind.DWord);
                    }

                    key.Flush();
                }
            }
            catch { }
        }
    }
}
