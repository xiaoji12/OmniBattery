using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using EasyBluetooth.Models;

namespace EasyBluetooth.UI
{
    public sealed class DeviceCardControl : Control
    {
        private DeviceInfo _device;
        private bool _isHovered;

        public DeviceInfo Device
        {
            get { return _device; }
            set
            {
                _device = value;
                Invalidate();
            }
        }

        private readonly Action<DeviceInfo> _onDeviceUpdated;

        public DeviceCardControl(DeviceInfo device, Action<DeviceInfo> onDeviceUpdated = null)
        {
            _device = device;
            _onDeviceUpdated = onDeviceUpdated;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            Height = 88;
            Cursor = Cursors.Hand;

            var cm = new ContextMenuStrip();
            cm.Items.Add("🔋 校准电量与充电状态...", null, (s, e) => OpenCalibration());
            ContextMenuStrip = cm;

            DoubleClick += (s, e) => OpenCalibration();
        }

        public void OpenCalibration()
        {
            if (_device == null) return;
            using (var dlg = new BatteryCalibrationDialog(_device))
            {
                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    Invalidate();
                    if (_onDeviceUpdated != null)
                        _onDeviceUpdated(_device);
                }
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // 1. Draw Card Background (WinUI 3 Fluent Card 8px corner)
            Color bgColor = _isHovered ? Theme.CardHover : Theme.CardBg;
            Color borderColor = _isHovered ? Theme.CardBorderHover : Theme.CardBorder;
            using (var path = Theme.CreateRoundedRectangle(rect, 8))
            {
                using (var brush = new SolidBrush(bgColor))
                {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(borderColor, 1.0f))
                {
                    g.DrawPath(pen, path);
                }
            }

            if (_device == null) return;

            // 2. Draw Device Vector Icon (WinUI 3 44x44 Icon Container)
            int iconBoxSize = 44;
            DrawDeviceVectorIcon(g, 16, (Height - iconBoxSize) / 2, iconBoxSize, iconBoxSize);

            // 3. Draw Device Info
            int textX = 72;
            int maxTextW = Math.Max(120, Width - textX - 85);

            // Title
            int textY = 14;
            using (var brush = new SolidBrush(Theme.TextPrimary))
            {
                StringFormat sf = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                Rectangle titleRect = new Rectangle(textX, textY, maxTextW, 22);
                g.DrawString(_device.DisplayName, Theme.FontDeviceName, brush, titleRect, sf);
            }

            // Badges (Brand + ConnectionType)
            int badgeY = textY + 23;
            int curBadgeX = textX;

            curBadgeX = DrawBadge(g, curBadgeX, badgeY, _device.Brand, Color.FromArgb(48, 54, 66), Color.FromArgb(147, 197, 253));
            curBadgeX = DrawBadge(g, curBadgeX + 6, badgeY, _device.ConnectionType, Color.FromArgb(36, 52, 44), Color.FromArgb(110, 231, 183));

            if (_device.IsCharging)
            {
                curBadgeX = DrawBadge(g, curBadgeX + 6, badgeY, "⚡ 充电中", Color.FromArgb(32, 58, 76), Color.FromArgb(96, 205, 255));
            }

            // Subtitle: Status & time
            int infoY = badgeY + 21;
            using (var brush = new SolidBrush(Theme.TextMuted))
            {
                string infoStr = string.Format("🟢 已连接 · 更新于 {0:HH:mm:ss} · 双击校准", _device.LastUpdated);
                StringFormat sf = new StringFormat
                {
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };
                Rectangle infoRect = new Rectangle(textX, infoY, maxTextW, 18);
                g.DrawString(infoStr, Theme.FontSmall, brush, infoRect, sf);
            }

            // 4. Draw Battery Ring Gauge (WinUI 3 Fluent Progress Ring)
            int gaugeSize = 48;
            int gaugeX = Width - gaugeSize - 20;
            int gaugeY = (Height - gaugeSize) / 2;
            DrawBatteryGauge(g, gaugeX, gaugeY, gaugeSize);
        }

        private int DrawBadge(Graphics g, int x, int y, string text, Color bg, Color fg)
        {
            if (string.IsNullOrEmpty(text)) return x;
            SizeF size = g.MeasureString(text, Theme.FontBadge);
            int w = (int)size.Width + 10;
            int h = (int)size.Height + 4;

            Rectangle badgeRect = new Rectangle(x, y, w, h);
            using (var path = Theme.CreateRoundedRectangle(badgeRect, 4))
            {
                using (var brush = new SolidBrush(bg))
                {
                    g.FillPath(brush, path);
                }
            }

            using (var brush = new SolidBrush(fg))
            {
                g.DrawString(text, Theme.FontBadge, brush, x + 5, y + 2);
            }

            return x + w;
        }

        private void DrawDeviceVectorIcon(Graphics g, int x, int y, int w, int h)
        {
            Rectangle box = new Rectangle(x, y, w, h);
            using (var path = Theme.CreateRoundedRectangle(box, 8))
            {
                using (var brush = new SolidBrush(Color.FromArgb(32, 32, 32)))
                {
                    g.FillPath(brush, path);
                }
                using (var pen = new Pen(Theme.CardBorder, 1f))
                {
                    g.DrawPath(pen, path);
                }
            }

            int cx = x + w / 2;
            int cy = y + h / 2;

            switch (_device.Category)
            {
                case DeviceCategory.Mouse:
                    using (var pen = new Pen(Color.FromArgb(96, 205, 255), 1.8f))
                    {
                        Rectangle mouseBody = new Rectangle(cx - 9, cy - 13, 18, 26);
                        using (var mPath = Theme.CreateRoundedRectangle(mouseBody, 7))
                        {
                            g.DrawPath(pen, mPath);
                        }
                        // Click separator line
                        g.DrawLine(pen, cx, cy - 13, cx, cy - 3);
                        // Wheel
                        using (var wBrush = new SolidBrush(Color.FromArgb(96, 205, 255)))
                        {
                            g.FillRectangle(wBrush, cx - 1.5f, cy - 9, 3f, 5f);
                        }
                    }
                    break;

                case DeviceCategory.Keyboard:
                    using (var pen = new Pen(Color.FromArgb(167, 139, 250), 1.8f))
                    {
                        Rectangle kbBody = new Rectangle(cx - 14, cy - 9, 28, 18);
                        using (var kPath = Theme.CreateRoundedRectangle(kbBody, 4))
                        {
                            g.DrawPath(pen, kPath);
                        }
                        // Spacebar
                        g.DrawLine(pen, cx - 6, cy + 4, cx + 6, cy + 4);
                        // Key dots
                        using (var kBrush = new SolidBrush(Color.FromArgb(167, 139, 250)))
                        {
                            g.FillRectangle(kBrush, cx - 9, cy - 4, 3, 3);
                            g.FillRectangle(kBrush, cx - 3, cy - 4, 3, 3);
                            g.FillRectangle(kBrush, cx + 3, cy - 4, 3, 3);
                            g.FillRectangle(kBrush, cx + 7, cy - 4, 2, 3);
                        }
                    }
                    break;

                case DeviceCategory.Headset:
                case DeviceCategory.AirPods:
                    using (var pen = new Pen(Color.FromArgb(244, 114, 182), 2.0f))
                    {
                        // Headband
                        g.DrawArc(pen, cx - 12, cy - 13, 24, 20, 180, 180);
                        // Earcups
                        using (var cupBrush = new SolidBrush(Color.FromArgb(244, 114, 182)))
                        {
                            Rectangle cupL = new Rectangle(cx - 14, cy - 2, 4, 11);
                            Rectangle cupR = new Rectangle(cx + 10, cy - 2, 4, 11);
                            using (var cpL = Theme.CreateRoundedRectangle(cupL, 2))
                            using (var cpR = Theme.CreateRoundedRectangle(cupR, 2))
                            {
                                g.FillPath(cupBrush, cpL);
                                g.FillPath(cupBrush, cpR);
                            }
                        }
                    }
                    break;

                case DeviceCategory.Gamepad:
                    using (var pen = new Pen(Color.FromArgb(52, 211, 153), 1.8f))
                    {
                        Rectangle gpBody = new Rectangle(cx - 13, cy - 8, 26, 16);
                        using (var gpPath = Theme.CreateRoundedRectangle(gpBody, 5))
                        {
                            g.DrawPath(pen, gpPath);
                        }
                        // D-pad & button dot
                        g.DrawLine(pen, cx - 7, cy, cx - 3, cy);
                        g.DrawLine(pen, cx - 5, cy - 2, cx - 5, cy + 2);
                        using (var bBrush = new SolidBrush(Color.FromArgb(52, 211, 153)))
                        {
                            g.FillEllipse(bBrush, cx + 4, cy - 2, 4, 4);
                        }
                    }
                    break;

                default:
                    using (var pen = new Pen(Color.FromArgb(160, 174, 192), 1.8f))
                    {
                        Rectangle genBody = new Rectangle(cx - 10, cy - 10, 20, 20);
                        using (var gnPath = Theme.CreateRoundedRectangle(genBody, 5))
                        {
                            g.DrawPath(pen, gnPath);
                        }
                    }
                    break;
            }
        }

        private void DrawBatteryGauge(Graphics g, int x, int y, int size)
        {
            int batteryVal = _device.Battery.HasValue ? _device.Battery.Value : 100;
            Color color = _device.Battery.HasValue ? Theme.GetBatteryColor(batteryVal, _device.IsCharging) : Theme.BatteryGreen;

            int padding = 4;
            Rectangle ringRect = new Rectangle(x + padding, y + padding, size - padding * 2, size - padding * 2);

            // WinUI 3 Track Background
            using (var pen = new Pen(Color.FromArgb(56, 56, 56), 4.2f))
            {
                g.DrawEllipse(pen, ringRect);
            }

            // WinUI 3 Progress Arc with Round Caps
            float sweepAngle = _device.Battery.HasValue ? ((batteryVal / 100f) * 360f) : 360f;
            if (sweepAngle > 0)
            {
                using (var pen = new Pen(color, 4.2f))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, ringRect, -90, sweepAngle);
                }
            }

            // Percentage Text in center
            string pctText = string.Format("{0}%", batteryVal);
            using (var brush = new SolidBrush(Theme.TextPrimary))
            {
                StringFormat sf = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                Rectangle textRect = new Rectangle(x, y + 1, size, size);
                g.DrawString(pctText, Theme.FontSubtitle, brush, textRect, sf);
            }
        }
    }
}
