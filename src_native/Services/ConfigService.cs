using System;
using System.IO;
using Microsoft.Win32;

namespace EasyBluetooth.Services
{
    public sealed class AppConfig
    {
        public int RefreshIntervalSeconds { get; set; }
        public bool EnableAida64 { get; set; }
        public bool EnableRtss { get; set; }
        public bool ShowFloatingWidget { get; set; }
        public int WidgetX { get; set; }
        public int WidgetY { get; set; }
        public bool StartWithWindows { get; set; }
        public bool MinimizeToTray { get; set; }

        public AppConfig()
        {
            RefreshIntervalSeconds = 3;
            EnableAida64 = false;
            EnableRtss = false;
            ShowFloatingWidget = false;
            WidgetX = 100;
            WidgetY = 100;
            StartWithWindows = false;
            MinimizeToTray = true;
        }
    }

    public static class ConfigService
    {
        private static readonly string ConfigDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasyBluetooth");
        private static readonly string ConfigFile = Path.Combine(ConfigDir, "config.ini");

        public static AppConfig Load()
        {
            var config = new AppConfig();
            try
            {
                if (!File.Exists(ConfigFile)) return config;

                string[] lines = File.ReadAllLines(ConfigFile);
                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;
                    int eq = line.IndexOf('=');
                    if (eq < 0) continue;

                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();

                    if (key.Equals("RefreshIntervalSeconds", StringComparison.OrdinalIgnoreCase))
                    {
                        int iv; if (int.TryParse(val, out iv)) config.RefreshIntervalSeconds = Math.Max(1, iv);
                    }
                    else if (key.Equals("EnableAida64", StringComparison.OrdinalIgnoreCase))
                    {
                        bool b; if (bool.TryParse(val, out b)) config.EnableAida64 = b;
                    }
                    else if (key.Equals("EnableRtss", StringComparison.OrdinalIgnoreCase))
                    {
                        bool b; if (bool.TryParse(val, out b)) config.EnableRtss = b;
                    }
                    else if (key.Equals("ShowFloatingWidget", StringComparison.OrdinalIgnoreCase))
                    {
                        bool b; if (bool.TryParse(val, out b)) config.ShowFloatingWidget = b;
                    }
                    else if (key.Equals("WidgetX", StringComparison.OrdinalIgnoreCase))
                    {
                        int v; if (int.TryParse(val, out v)) config.WidgetX = v;
                    }
                    else if (key.Equals("WidgetY", StringComparison.OrdinalIgnoreCase))
                    {
                        int v; if (int.TryParse(val, out v)) config.WidgetY = v;
                    }
                    else if (key.Equals("StartWithWindows", StringComparison.OrdinalIgnoreCase))
                    {
                        bool b; if (bool.TryParse(val, out b)) config.StartWithWindows = b;
                    }
                    else if (key.Equals("MinimizeToTray", StringComparison.OrdinalIgnoreCase))
                    {
                        bool b; if (bool.TryParse(val, out b)) config.MinimizeToTray = b;
                    }
                }
            }
            catch { }
            return config;
        }

        public static void Save(AppConfig config)
        {
            try
            {
                if (!Directory.Exists(ConfigDir)) Directory.CreateDirectory(ConfigDir);

                using (var sw = new StreamWriter(ConfigFile, false))
                {
                    sw.WriteLine("# EasyBluetooth Configuration");
                    sw.WriteLine("RefreshIntervalSeconds=" + config.RefreshIntervalSeconds);
                    sw.WriteLine("EnableAida64=" + config.EnableAida64);
                    sw.WriteLine("EnableRtss=" + config.EnableRtss);
                    sw.WriteLine("ShowFloatingWidget=" + config.ShowFloatingWidget);
                    sw.WriteLine("WidgetX=" + config.WidgetX);
                    sw.WriteLine("WidgetY=" + config.WidgetY);
                    sw.WriteLine("StartWithWindows=" + config.StartWithWindows);
                    sw.WriteLine("MinimizeToTray=" + config.MinimizeToTray);
                }

                // Handle Windows startup registry
                SetStartup(config.StartWithWindows);
            }
            catch { }
        }

        private static void SetStartup(bool enable)
        {
            try
            {
                string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                string shortcutCmd = Path.Combine(startupDir, "EasyBluetooth.cmd");
                if (enable)
                {
                    string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                    File.WriteAllText(shortcutCmd, string.Format("start \"\" \"{0}\" --tray\r\n", appPath));
                }
                else
                {
                    if (File.Exists(shortcutCmd)) File.Delete(shortcutCmd);
                }
            }
            catch { }
        }
    }
}
