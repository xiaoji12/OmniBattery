using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using EasyBluetooth.Models;

namespace EasyBluetooth.Services
{
    public static class BatteryStore
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

        private static readonly string CacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasyBluetooth");
        private static readonly string CacheFile = Path.Combine(CacheDir, "battery_cache.ini");

        private static readonly Dictionary<string, int> _batteryLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, bool> _chargingStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static bool _isLoaded = false;
        private static readonly object _lock = new object();

        private static void EnsureLoaded()
        {
            lock (_lock)
            {
                if (_isLoaded) return;
                _isLoaded = true;

                try
                {
                    if (!File.Exists(CacheFile)) return;

                    string currentSection = "";
                    string[] lines = File.ReadAllLines(CacheFile);
                    foreach (string rawLine in lines)
                    {
                        string line = rawLine.Trim();
                        if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith(";"))
                            continue;

                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            currentSection = line.Substring(1, line.Length - 2).Trim().ToLowerInvariant();
                            continue;
                        }

                        int eq = line.IndexOf('=');
                        if (eq <= 0) continue;

                        string key = line.Substring(0, eq).Trim();
                        string val = line.Substring(eq + 1).Trim();

                        if (currentSection == "battery")
                        {
                            int b;
                            if (int.TryParse(val, out b))
                            {
                                _batteryLevels[key] = Math.Max(1, Math.Min(100, b));
                            }
                        }
                        else if (currentSection == "charging")
                        {
                            bool c;
                            if (bool.TryParse(val, out c))
                            {
                                _chargingStates[key] = c;
                            }
                        }
                    }
                }
                catch { }
            }
        }

        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    if (!Directory.Exists(CacheDir))
                        Directory.CreateDirectory(CacheDir);

                    using (var sw = new StreamWriter(CacheFile, false, System.Text.Encoding.UTF8))
                    {
                        sw.WriteLine("# EasyBluetooth 2.4G & Wireless Battery Cache & Calibration");
                        sw.WriteLine("[Battery]");
                        foreach (var kvp in _batteryLevels)
                        {
                            sw.WriteLine(string.Format("{0}={1}", kvp.Key, kvp.Value));
                        }
                        sw.WriteLine();
                        sw.WriteLine("[Charging]");
                        foreach (var kvp in _chargingStates)
                        {
                            sw.WriteLine(string.Format("{0}={1}", kvp.Key, kvp.Value));
                        }
                    }
                }
                catch { }
            }
        }

        public static int? GetBattery(string deviceName, string displayName)
        {
            EnsureLoaded();

            string primaryKey = !string.IsNullOrEmpty(displayName) ? displayName : deviceName;

            lock (_lock)
            {
                int val;
                if (_batteryLevels.TryGetValue(primaryKey, out val))
                    return val;
                if (!string.IsNullOrEmpty(deviceName) && _batteryLevels.TryGetValue(deviceName, out val))
                    return val;
                return null;
            }
        }

        public static bool IsCharging(string deviceName, string displayName)
        {
            EnsureLoaded();
            string primaryKey = !string.IsNullOrEmpty(displayName) ? displayName : deviceName;

            lock (_lock)
            {
                bool c;
                if (_chargingStates.TryGetValue(primaryKey, out c))
                    return c;
                if (!string.IsNullOrEmpty(deviceName) && _chargingStates.TryGetValue(deviceName, out c))
                    return c;
                return false;
            }
        }

        public static void SetBattery(string deviceKey, int battery, bool isCharging)
        {
            EnsureLoaded();
            battery = Math.Max(1, Math.Min(100, battery));

            lock (_lock)
            {
                _batteryLevels[deviceKey] = battery;
                _chargingStates[deviceKey] = isCharging;
                Save();
            }
        }

        public static int? TryQueryHidBattery(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                // GENERIC_READ (0x80000000) | GENERIC_WRITE (0x40000000)
                using (var handle = CreateFile(path, 0xC0000000, 1 | 2, IntPtr.Zero, 3, 0, IntPtr.Zero))
                {
                    if (handle.IsInvalid) return null;

                    // Try input report 0
                    byte[] buf = new byte[65];
                    buf[0] = 0;
                    if (HidD_GetInputReport(handle, buf, (uint)buf.Length))
                    {
                        // Check if byte 1 has reasonable battery value (10 - 100)
                        if (buf[1] >= 10 && buf[1] <= 100)
                        {
                            return (int)buf[1];
                        }
                    }

                    // Try feature report 0 and 7
                    byte[] feat = new byte[65];
                    feat[0] = 7;
                    if (HidD_GetFeature(handle, feat, (uint)feat.Length))
                    {
                        if (feat[1] >= 10 && feat[1] <= 100)
                        {
                            return (int)feat[1];
                        }
                    }
                }
            }
            catch { }
            return null;
        }
    }
}
