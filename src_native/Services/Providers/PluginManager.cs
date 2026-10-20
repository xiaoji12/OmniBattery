using System;
using System.Collections.Generic;
using EasyBluetooth.Models;
using EasyBluetooth.Services.Providers.Logitech;
using EasyBluetooth.Services.Providers.Antgamer;
using EasyBluetooth.Services.Providers.Pmo;
using EasyBluetooth.Services.Providers.Rawm;
using EasyBluetooth.Services.Providers.Razer;
using EasyBluetooth.Services.Providers.Colorful;
using EasyBluetooth.Services.Providers.Dareu;

namespace EasyBluetooth.Services.Providers
{
    public static class PluginManager
    {
        private static readonly List<IDevicePlugin> _plugins = new List<IDevicePlugin>();
        private static readonly object _lock = new object();

        static PluginManager()
        {
            InitializePlugins();
        }

        private static void InitializePlugins()
        {
            lock (_lock)
            {
                _plugins.Clear();

                // Official EasyBluetooth VIP 2.4G Addon Plugins (All Enabled)
                _plugins.Add(new AntgamerPlugin());
                _plugins.Add(new LogitechPlugin());
                _plugins.Add(new DareuPlugin());
                _plugins.Add(new PmoPlugin());
                _plugins.Add(new RawmPlugin());
                _plugins.Add(new RazerPlugin());
                _plugins.Add(new ColorfulPlugin());
            }
        }

        public static IList<IDevicePlugin> GetPlugins()
        {
            lock (_lock)
            {
                return _plugins.AsReadOnly();
            }
        }

        public static bool TryQueryBattery(string devicePath, string productName, string displayName, string brand, DeviceCategory category, out int batteryLevel, out bool isCharging)
        {
            batteryLevel = -1;
            isCharging = false;

            // 1. Try VIP hardware protocol providers
            lock (_lock)
            {
                foreach (var plugin in _plugins)
                {
                    if (!plugin.IsEnabled || plugin.Provider == null) continue;

                    if (plugin.Provider.CanHandle(devicePath, productName, brand))
                    {
                        int bat;
                        bool chg;
                        if (plugin.Provider.TryQueryBattery(devicePath, out bat, out chg))
                        {
                            if (bat >= 0 && bat <= 100)
                            {
                                batteryLevel = bat;
                                isCharging = chg;
                                BatteryStore.SetBattery(!string.IsNullOrEmpty(displayName) ? displayName : productName, bat, chg);
                                return true;
                            }
                        }
                    }
                }
            }

            // If hardware query failed / timed out, the peripheral is OFF or disconnected.
            // DO NOT fall back to fake default values!
            return false;
        }
    }
}
