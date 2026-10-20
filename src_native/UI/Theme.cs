using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace EasyBluetooth.UI
{
    public static class Theme
    {
        // WinUI 3 / Windows 11 Dark Mode Palette (Mica & Acrylic)
        public static readonly Color BgDark = Color.FromArgb(32, 32, 32);             // Mica Alt Base
        public static readonly Color BgHeader = Color.FromArgb(32, 32, 32);           // Seamless TitleBar
        public static readonly Color HeaderBorder = Color.FromArgb(48, 48, 48);       // 1px Subtle Bottom Border
        
        public static readonly Color CardBg = Color.FromArgb(43, 43, 43);             // Fluent Card Dark
        public static readonly Color CardHover = Color.FromArgb(50, 50, 50);          // Card Hover Highlight
        public static readonly Color CardBorder = Color.FromArgb(58, 58, 58);         // Card 1px Subtle Border
        public static readonly Color CardBorderHover = Color.FromArgb(75, 75, 75);

        public static readonly Color TextPrimary = Color.FromArgb(255, 255, 255);      // Pure White Text
        public static readonly Color TextSecondary = Color.FromArgb(208, 208, 208);    // Subtext
        public static readonly Color TextMuted = Color.FromArgb(145, 145, 145);        // Caption
        public static readonly Color TextCaption = Color.FromArgb(115, 115, 115);

        public static readonly Color Accent = Color.FromArgb(96, 205, 255);           // WinUI 3 Accent Blue (Cyan-Blue)
        public static readonly Color AccentPurple = Color.FromArgb(135, 94, 255);     // Fluent Purple Logo
        public static readonly Color AccentHover = Color.FromArgb(120, 215, 255);

        // WinUI 3 Battery & Status Indicator Colors
        public static readonly Color BatteryGreen = Color.FromArgb(108, 203, 95);      // Fluent Green
        public static readonly Color BatteryOrange = Color.FromArgb(252, 175, 23);     // Fluent Orange
        public static readonly Color BatteryRed = Color.FromArgb(255, 153, 164);       // Fluent Soft Red
        public static readonly Color BatteryCharging = Color.FromArgb(96, 205, 255);   // Fluent Charging Cyan

        private static readonly string FontFamilyName = GetBestFontFamily();

        public static readonly Font FontTitle = new Font(FontFamilyName, 13.5f, FontStyle.Bold);
        public static readonly Font FontSubtitle = new Font(FontFamilyName, 9.5f, FontStyle.Regular);
        public static readonly Font FontDeviceName = new Font(FontFamilyName, 11f, FontStyle.Bold);
        public static readonly Font FontBadge = new Font(FontFamilyName, 8.5f, FontStyle.Regular);
        public static readonly Font FontBattery = new Font(FontFamilyName, 13.5f, FontStyle.Bold);
        public static readonly Font FontSmall = new Font(FontFamilyName, 8.5f, FontStyle.Regular);

        private static string GetBestFontFamily()
        {
            try
            {
                using (var test = new Font("Segoe UI Variable Display", 10f))
                {
                    if (test.Name.Equals("Segoe UI Variable Display", StringComparison.OrdinalIgnoreCase))
                        return "Segoe UI Variable Display";
                }
            }
            catch { }

            try
            {
                using (var test = new Font("Segoe UI", 10f))
                {
                    if (test.Name.Equals("Segoe UI", StringComparison.OrdinalIgnoreCase))
                        return "Segoe UI";
                }
            }
            catch { }

            return "Microsoft YaHei UI";
        }

        public static Color GetBatteryColor(int battery, bool isCharging)
        {
            if (isCharging) return BatteryCharging;
            if (battery >= 50) return BatteryGreen;
            if (battery >= 20) return BatteryOrange;
            return BatteryRed;
        }

        public static GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            if (diameter > rect.Width) diameter = rect.Width;
            if (diameter > rect.Height) diameter = rect.Height;
            Rectangle arc = new Rectangle(rect.Location, new Size(diameter, diameter));

            // top left
            path.AddArc(arc, 180, 90);

            // top right
            arc.X = rect.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right
            arc.Y = rect.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left
            arc.X = rect.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }
    }
}
