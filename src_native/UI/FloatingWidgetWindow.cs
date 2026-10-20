using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EasyBluetooth.Models;

namespace EasyBluetooth.UI
{
    public sealed class FloatingWidgetWindow : Form
    {
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        private List<DeviceInfo> _devices = new List<DeviceInfo>();
        private readonly Action _onOpenMain;

        public FloatingWidgetWindow(Action onOpenMain)
        {
            _onOpenMain = onOpenMain;

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = Color.FromArgb(18, 18, 26);
            DoubleBuffered = true;
            Width = 260;
            Height = 44;

            MouseDown += FloatingWidgetWindow_MouseDown;
            DoubleClick += (s, e) => { if (_onOpenMain != null) _onOpenMain(); };

            var cm = new ContextMenuStrip();
            cm.Items.Add("🖥️ 打开主窗口", null, (s, e) => { if (_onOpenMain != null) _onOpenMain(); });
            cm.Items.Add("✕ 隐藏悬浮窗", null, (s, e) => Hide());
            ContextMenuStrip = cm;
        }

        private void FloatingWidgetWindow_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
            }
        }

        public void UpdateDevices(List<DeviceInfo> devices)
        {
            _devices = devices ?? new List<DeviceInfo>();

            // Auto resize based on devices count
            int minWidth = Math.Max(160, 40 + _devices.Count * 90);
            Width = Math.Min(480, minWidth);

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = Theme.CreateRoundedRectangle(rect, 14))
            {
                using (var brush = new SolidBrush(Color.FromArgb(230, 24, 24, 36)))
                {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(Color.FromArgb(80, 99, 102, 241), 1.5f))
                {
                    g.DrawPath(pen, path);
                }
            }

            if (_devices.Count == 0)
            {
                using (var brush = new SolidBrush(Theme.TextMuted))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("暂无已连接设备", Theme.FontSmall, brush, rect, sf);
                }
                return;
            }

            int curX = 14;
            int itemY = 10;

            for (int i = 0; i < _devices.Count; i++)
            {
                var dev = _devices[i];
                string icon = "📱";
                switch (dev.Category)
                {
                    case DeviceCategory.Mouse: icon = "🖱️"; break;
                    case DeviceCategory.Keyboard: icon = "⌨️"; break;
                    case DeviceCategory.Headset: icon = "🎧"; break;
                    case DeviceCategory.Gamepad: icon = "🎮"; break;
                    case DeviceCategory.AirPods: icon = "🎧"; break;
                }

                // Draw emoji icon
                using (var f = new Font("Segoe UI Emoji", 11f, FontStyle.Regular))
                {
                    using (var brush = new SolidBrush(Color.White))
                    {
                        g.DrawString(icon, f, brush, curX, itemY);
                    }
                }
                curX += 24;

                // Battery text with color
                int batVal = dev.Battery.HasValue ? dev.Battery.Value : 0;
                Color batCol = Theme.GetBatteryColor(batVal, dev.IsCharging);
                string batStr = (dev.IsCharging ? "⚡" : "") + (dev.Battery.HasValue ? dev.Battery.Value + "%" : "--");

                using (var f = new Font("Microsoft YaHei UI", 9.5f, FontStyle.Bold))
                {
                    using (var brush = new SolidBrush(batCol))
                    {
                        g.DrawString(batStr, f, brush, curX, itemY + 1);
                        SizeF sz = g.MeasureString(batStr, f);
                        curX += (int)sz.Width + 12;
                    }
                }

                if (i < _devices.Count - 1)
                {
                    using (var pen = new Pen(Color.FromArgb(60, 255, 255, 255)))
                    {
                        g.DrawLine(pen, curX - 6, 12, curX - 6, Height - 12);
                    }
                }
            }
        }
    }
}
