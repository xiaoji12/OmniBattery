using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using EasyBluetooth.Models;
using EasyBluetooth.Services;

namespace EasyBluetooth.UI
{
    public sealed class TrayManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly Action _onOpenMain;
        private readonly Action _onToggleWidget;
        private readonly Action _onRefresh;
        private readonly Action _onExit;
        private readonly AppConfig _config;

        private ToolStripMenuItem _itemWidget;
        private ToolStripMenuItem _itemStartup;

        public TrayManager(AppConfig config, Action onOpenMain, Action onToggleWidget, Action onRefresh, Action onExit)
        {
            _config = config;
            _onOpenMain = onOpenMain;
            _onToggleWidget = onToggleWidget;
            _onRefresh = onRefresh;
            _onExit = onExit;

            _notifyIcon = new NotifyIcon
            {
                Text = "OmniBattery v1.0.0",
                Icon = CreateAppIcon(),
                Visible = true
            };

            _notifyIcon.DoubleClick += (s, e) => { if (_onOpenMain != null) _onOpenMain(); };
            _notifyIcon.Click += (s, e) =>
            {
                var me = e as MouseEventArgs;
                if (me != null && me.Button == MouseButtons.Left)
                {
                    if (_onOpenMain != null) _onOpenMain();
                }
            };

            BuildContextMenu();
        }

        private void BuildContextMenu()
        {
            var cm = new ContextMenuStrip();

            var itemOpen = new ToolStripMenuItem("🖥️ 打开主界面", null, (s, e) => { if (_onOpenMain != null) _onOpenMain(); });
            itemOpen.Font = new Font(itemOpen.Font, FontStyle.Bold);
            cm.Items.Add(itemOpen);

            _itemWidget = new ToolStripMenuItem("📌 桌面悬浮窗", null, (s, e) =>
            {
                if (_onToggleWidget != null) _onToggleWidget();
                UpdateWidgetChecked(_config.ShowFloatingWidget);
            });
            _itemWidget.Checked = _config.ShowFloatingWidget;
            cm.Items.Add(_itemWidget);

            cm.Items.Add(new ToolStripSeparator());

            cm.Items.Add("🔄 立即刷新设备", null, (s, e) => { if (_onRefresh != null) _onRefresh(); });

            _itemStartup = new ToolStripMenuItem("⚙️ 开机自动启动", null, (s, e) =>
            {
                _config.StartWithWindows = !_config.StartWithWindows;
                _itemStartup.Checked = _config.StartWithWindows;
                ConfigService.Save(_config);
            });
            _itemStartup.Checked = _config.StartWithWindows;
            cm.Items.Add(_itemStartup);

            cm.Items.Add(new ToolStripSeparator());

            cm.Items.Add("🚪 退出 OmniBattery", null, (s, e) => { if (_onExit != null) _onExit(); });

            _notifyIcon.ContextMenuStrip = cm;
        }

        public void UpdateWidgetChecked(bool isChecked)
        {
            if (_itemWidget != null) _itemWidget.Checked = isChecked;
        }

        public void UpdateDevices(List<DeviceInfo> devices)
        {
            if (devices == null || devices.Count == 0)
            {
                _notifyIcon.Text = "EasyBluetooth: 无已连接设备";
                return;
            }

            var sb = new StringBuilder();
            sb.Append("EasyBluetooth:");
            foreach (var d in devices)
            {
                string line = string.Format("\n{0}: {1}", d.DisplayName, d.GetBatteryDisplay());
                if (sb.Length + line.Length < 63)
                {
                    sb.Append(line);
                }
            }

            string text = sb.ToString();
            if (text.Length > 63) text = text.Substring(0, 60) + "...";
            _notifyIcon.Text = text;
        }

        public static Icon CreateAppIcon()
        {
            using (var bmp = new Bitmap(32, 32))
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                    // Rounded dark blue/indigo circle
                    using (var brush = new SolidBrush(Color.FromArgb(99, 102, 241)))
                    {
                        g.FillEllipse(brush, 1, 1, 30, 30);
                    }

                    // Battery / Lightning symbol
                    using (var pen = new Pen(Color.White, 2.2f))
                    {
                        // Draw lightning bolt
                        Point[] pts = new Point[]
                        {
                            new Point(17, 6),
                            new Point(11, 16),
                            new Point(16, 16),
                            new Point(15, 26),
                            new Point(22, 14),
                            new Point(17, 14)
                        };
                        using (var brush = new SolidBrush(Color.White))
                        {
                            g.FillPolygon(brush, pts);
                        }
                    }
                }
                return Icon.FromHandle(bmp.GetHicon());
            }
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
        }
    }
}
