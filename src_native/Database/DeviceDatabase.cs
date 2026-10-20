using System;
using System.Collections.Generic;
using EasyBluetooth.Models;

namespace EasyBluetooth.Database
{
    public sealed class DeviceModelEntry
    {
        public string Brand { get; set; }
        public string Model { get; set; }
        public DeviceCategory Category { get; set; }
        public string Keyword { get; set; }
    }

    public static class DeviceDatabase
    {
        private static readonly List<DeviceModelEntry> Models = new List<DeviceModelEntry>();

        static DeviceDatabase()
        {
            // Apple
            Add("Apple/苹果", "AirPods", DeviceCategory.AirPods, "AirPods");
            Add("Apple/苹果", "AirPods Pro", DeviceCategory.AirPods, "AirPods Pro");
            Add("Apple/苹果", "AirPods Max", DeviceCategory.AirPods, "AirPods Max");

            // Razer
            Add("Razer/雷蛇", "雷蛇 八岐大蛇 V2 (Orochi V2)", DeviceCategory.Mouse, "Orochi");
            Add("Razer/雷蛇", "雷蛇 毒蝰 (Viper)", DeviceCategory.Mouse, "Viper");
            Add("Razer/雷蛇", "雷蛇 炼狱蝰蛇 (DeathAdder)", DeviceCategory.Mouse, "DeathAdder");
            Add("Razer/雷蛇", "雷蛇 巴塞利斯蛇 (Basilisk)", DeviceCategory.Mouse, "Basilisk");
            Add("Razer/雷蛇", "雷蛇 眼镜蛇 (Cobra)", DeviceCategory.Mouse, "Cobra");
            Add("Razer/雷蛇", "雷蛇 猎魂光蛛 (Huntsman)", DeviceCategory.Keyboard, "Huntsman");
            Add("Razer/雷蛇", "雷蛇 黑寡妇 (BlackWidow)", DeviceCategory.Keyboard, "BlackWidow");
            Add("Razer/雷蛇", "雷蛇 噬魂金蝎 (DeathStalker)", DeviceCategory.Keyboard, "DeathStalker");
            Add("Razer/雷蛇", "雷蛇 旋风黑鲨 (BlackShark)", DeviceCategory.Headset, "BlackShark");
            Add("Razer/雷蛇", "雷蛇 梭鱼 (Barracuda)", DeviceCategory.Headset, "Barracuda");
            Add("Razer/雷蛇", "雷蛇 设备", DeviceCategory.General, "Razer");

            // Logitech
            Add("Logitech/罗技", "罗技 G304", DeviceCategory.Mouse, "G304");
            Add("Logitech/罗技", "罗技 GPW / G PRO Wireless", DeviceCategory.Mouse, "G PRO");
            Add("Logitech/罗技", "罗技 GPW 2 / Superlight", DeviceCategory.Mouse, "Superlight");
            Add("Logitech/罗技", "罗技 G502 LIGHTSPEED", DeviceCategory.Mouse, "G502");
            Add("Logitech/罗技", "罗技 G903 LIGHTSPEED", DeviceCategory.Mouse, "G903");
            Add("Logitech/罗技", "罗技 G913 / G915 无线键盘", DeviceCategory.Keyboard, "G913");
            Add("Logitech/罗技", "罗技 G915 无线键盘", DeviceCategory.Keyboard, "G915");
            Add("Logitech/罗技", "罗技 G733 无线耳机", DeviceCategory.Headset, "G733");
            Add("Logitech/罗技", "罗技 G435 无线耳机", DeviceCategory.Headset, "G435");
            Add("Logitech/罗技", "罗技 PRO X 无线游戏耳机", DeviceCategory.Headset, "PRO X");
            Add("Logitech/罗技", "罗技 ASTRO A50", DeviceCategory.Headset, "A50");
            Add("Logitech/罗技", "罗技 MX Master", DeviceCategory.Mouse, "MX Master");
            Add("Logitech/罗技", "罗技 MX Keys", DeviceCategory.Keyboard, "MX Keys");
            Add("Logitech/罗技", "罗技 设备", DeviceCategory.General, "Logitech");

            // VGN & ATK
            Add("VGN", "VGN 蜻蜓 F1", DeviceCategory.Mouse, "Dragonfly");
            Add("VGN", "VGN 蜻蜓 3 Master", DeviceCategory.Mouse, "Dragonfly 3");
            Add("VGN", "VGN V98 Pro", DeviceCategory.Keyboard, "V98");
            Add("VGN", "VGN V87", DeviceCategory.Keyboard, "V87");
            Add("VGN", "VGN 设备", DeviceCategory.General, "VGN");
            Add("ATK", "ATK X1", DeviceCategory.Mouse, "ATK X1");
            Add("ATK", "ATK 烈空 幻影", DeviceCategory.Mouse, "Blazing Sky");
            Add("ATK", "ATK 设备", DeviceCategory.General, "ATK");

            // MCHOSE / 迈从
            Add("MCHOSE/迈从", "迈从 K99", DeviceCategory.Keyboard, "K99");
            Add("MCHOSE/迈从", "迈从 G98", DeviceCategory.Keyboard, "G98");
            Add("MCHOSE/迈从", "迈从 V9 Pro", DeviceCategory.Mouse, "V9 Pro");
            Add("MCHOSE/迈从", "迈从 R9", DeviceCategory.Mouse, "R9");
            Add("MCHOSE/迈从", "迈从 设备", DeviceCategory.General, "MCHOSE");

            // Dareu / 达尔优
            Add("Dareu/达尔优", "达尔优 A98 三模键盘", DeviceCategory.Keyboard, "A98");
            Add("Dareu/达尔优", "达尔优 A87 三模键盘", DeviceCategory.Keyboard, "A87");
            Add("Dareu/达尔优", "达尔优 A950", DeviceCategory.Mouse, "A950");
            Add("Dareu/达尔优", "达尔优 EM901", DeviceCategory.Mouse, "EM901");
            Add("Dareu/达尔优", "达尔优 设备", DeviceCategory.General, "Dareu");

            // ASUS ROG
            Add("ASUS/华硕", "ROG 龙鳞 Harpe Ace", DeviceCategory.Mouse, "Harpe");
            Add("ASUS/华硕", "ROG 月刃 Keris", DeviceCategory.Mouse, "Keris");
            Add("ASUS/华硕", "ROG 大理石 Gladius", DeviceCategory.Mouse, "Gladius");
            Add("ASUS/华硕", "ROG 夜魔 Azoth", DeviceCategory.Keyboard, "Azoth");
            Add("ASUS/华硕", "ROG 魔导士 Falchion", DeviceCategory.Keyboard, "Falchion");
            Add("ASUS/华硕", "ROG 棱镜 Delta", DeviceCategory.Headset, "Delta");
            Add("ASUS/华硕", "ROG 设备", DeviceCategory.General, "ROG");

            // SteelSeries
            Add("Steelseries/赛睿", "赛睿 寒冰新星 7 (Nova 7)", DeviceCategory.Headset, "Nova 7");
            Add("Steelseries/赛睿", "赛睿 寒冰 7+ (Arctis 7+)", DeviceCategory.Headset, "Arctis 7");
            Add("Steelseries/赛睿", "赛睿 Prime Wireless", DeviceCategory.Mouse, "Prime");
            Add("Steelseries/赛睿", "赛睿 Aerox", DeviceCategory.Mouse, "Aerox");
            Add("Steelseries/赛睿", "赛睿 设备", DeviceCategory.General, "SteelSeries");

            // Gamepads
            Add("Microsoft/微软", "Xbox 无线控制器", DeviceCategory.Gamepad, "Xbox");
            Add("Sony/索尼", "PS5 DualSense 无线控制器", DeviceCategory.Gamepad, "DualSense");
            Add("Sony/索尼", "PS4 DualShock 无线控制器", DeviceCategory.Gamepad, "DualShock");
            Add("8BitDo/八位堂", "八位堂 猎手/Ultimate 手柄", DeviceCategory.Gamepad, "Ultimate");
            Add("8BitDo/八位堂", "八位堂 Retro87", DeviceCategory.Keyboard, "Retro87");
            Add("Flydigi/飞智", "飞智 八爪鱼4", DeviceCategory.Gamepad, "Octopus");
            Add("BETOP/北通", "北通 阿修罗2 Pro+", DeviceCategory.Gamepad, "Asura");
            Add("Gamesir/盖世小鸡", "盖世小鸡 九尾狐", DeviceCategory.Gamepad, "Gamesir");

            // Audio & Peripherals
            Add("Edifier/漫步者", "漫步者 G5M Pro", DeviceCategory.Mouse, "G5M Pro");
            Add("Edifier/漫步者", "漫步者 蓝牙耳机", DeviceCategory.Headset, "Edifier");
            Add("Sony/索尼", "索尼 WH-1000XM 耳机", DeviceCategory.Headset, "WH-1000");
            Add("Sony/索尼", "索尼 WF-1000XM 耳机", DeviceCategory.Headset, "WF-1000");
            Add("Sony/索尼", "索尼 INZONE 耳机", DeviceCategory.Headset, "INZONE");
            Add("AULA/狼蛛", "狼蛛 F75 / F87 / F99", DeviceCategory.Keyboard, "AULA");
            Add("Leobog/莱奥伯格", "LEOBOG Hi75 / Hi98", DeviceCategory.Keyboard, "LEOBOG");
            Add("DarmoShark/达摩鲨", "达摩鲨 M2 / M3 Pro", DeviceCategory.Mouse, "DarmoShark");
            Add("Corsair/海盗船", "海盗船 K100 Air", DeviceCategory.Keyboard, "K100 Air");
            Add("Corsair/海盗船", "海盗船 无线设备", DeviceCategory.General, "Corsair");

            // ANTGAMER & COLORFUL
            Add("ANTGAMER/蚂蚁电竞", "蚂蚁电竞 AGM535R 2.4G", DeviceCategory.Mouse, "AGM535R");
            Add("ANTGAMER/蚂蚁电竞", "蚂蚁电竞 AGM535R 2.4G", DeviceCategory.Mouse, "AGM535");
            Add("COLORFUL/七彩虹", "七彩虹 COLORFUL 2.4G 键盘", DeviceCategory.Keyboard, "COLORFUL");
        }

        private static void Add(string brand, string model, DeviceCategory cat, string keyword)
        {
            Models.Add(new DeviceModelEntry
            {
                Brand = brand,
                Model = model,
                Category = cat,
                Keyword = keyword
            });
        }

        public static void Identify(string rawName, out string brand, out string displayName, out DeviceCategory category)
        {
            Identify(rawName, null, out brand, out displayName, out category);
        }

        public static void Identify(string rawName, string devicePath, out string brand, out string displayName, out DeviceCategory category)
        {
            brand = "通用外设";
            displayName = !string.IsNullOrEmpty(rawName) ? rawName : "2.4G 无线设备";
            category = GuessCategoryFromName(rawName);

            // 1. Direct hardware VID / PID identification
            if (!string.IsNullOrEmpty(devicePath))
            {
                if (devicePath.IndexOf("VID_260D", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    brand = "Dareu/达尔优";
                    displayName = "达尔优无线键盘 (A98)";
                    category = DeviceCategory.Keyboard;
                    return;
                }
                if (devicePath.IndexOf("VID_046D&PID_0ADF", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    brand = "Logitech/罗技";
                    displayName = "罗技 G435 无线游戏耳机";
                    category = DeviceCategory.Headset;
                    return;
                }
                if (devicePath.IndexOf("VID_0BDA&PID_FFE7", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    brand = "ANTGAMER/蚂蚁电竞";
                    displayName = "蚂蚁电竞 AGM535R 2.4G 鼠标";
                    category = DeviceCategory.Mouse;
                    return;
                }
                if (devicePath.IndexOf("VID_2F4C&PID_1000", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    brand = "COLORFUL/七彩虹";
                    displayName = "七彩虹 COLORFUL 2.4G 键盘";
                    category = DeviceCategory.Keyboard;
                    return;
                }
            }

            if (string.IsNullOrEmpty(rawName)) return;

            string lower = rawName.ToLowerInvariant();

            foreach (var entry in Models)
            {
                if (lower.IndexOf(entry.Keyword.ToLowerInvariant()) >= 0)
                {
                    brand = entry.Brand;
                    displayName = entry.Model;
                    category = entry.Category;
                    return;
                }
            }

            // Fallback heuristics
            if (lower.Contains("mouse") || lower.Contains("鼠标") || lower.Contains("mice"))
            {
                category = DeviceCategory.Mouse;
            }
            else if (lower.Contains("keyboard") || lower.Contains("键盘") || lower.Contains("kb"))
            {
                category = DeviceCategory.Keyboard;
            }
            else if (lower.Contains("headset") || lower.Contains("headphone") || lower.Contains("earphone") || lower.Contains("audio") || lower.Contains("buds") || lower.Contains("耳机"))
            {
                category = DeviceCategory.Headset;
            }
            else if (lower.Contains("gamepad") || lower.Contains("controller") || lower.Contains("手柄"))
            {
                category = DeviceCategory.Gamepad;
            }
        }

        public static bool IsKnownWirelessModel(string rawName, out string brand, out string displayName, out DeviceCategory category)
        {
            Identify(rawName, out brand, out displayName, out category);
            return !string.Equals(brand, "通用外设", StringComparison.OrdinalIgnoreCase);
        }

        private static DeviceCategory GuessCategoryFromName(string name)
        {
            if (string.IsNullOrEmpty(name)) return DeviceCategory.General;
            string l = name.ToLowerInvariant();
            if (l.Contains("mouse") || l.Contains("鼠标")) return DeviceCategory.Mouse;
            if (l.Contains("keyboard") || l.Contains("键盘")) return DeviceCategory.Keyboard;
            if (l.Contains("headset") || l.Contains("headphone") || l.Contains("audio") || l.Contains("耳机")) return DeviceCategory.Headset;
            if (l.Contains("controller") || l.Contains("gamepad") || l.Contains("手柄") || l.Contains("xinput")) return DeviceCategory.Gamepad;
            if (l.Contains("airpods")) return DeviceCategory.AirPods;
            return DeviceCategory.General;
        }
    }
}
