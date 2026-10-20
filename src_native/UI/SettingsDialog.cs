using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using EasyBluetooth.Services;

namespace EasyBluetooth.UI
{
    public sealed class SettingsDialog : Form
    {
        private readonly AppConfig _config;
        private ComboBox _cboInterval;
        private CheckBox _chkAutoStart;
        private CheckBox _chkMinToTray;
        private CheckBox _chkFloatingWidget;

        public SettingsDialog(AppConfig config)
        {
            _config = config;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "OmniBattery 设置";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 390);
            BackColor = Theme.BgDark;
            DoubleBuffered = true;

            // 1. Header
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 52,
                BackColor = Theme.BgHeader
            };
            header.Paint += (s, e) =>
            {
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
                using (var brush = new SolidBrush(Theme.TextPrimary))
                {
                    g.DrawString("⚙️ 参数设置", Theme.FontDeviceName, brush, 18, 15);
                }
                using (var vBrush = new SolidBrush(Theme.TextSecondary))
                {
                    g.DrawString("v1.0.0", Theme.FontSmall, vBrush, 126, 19);
                }
            };
            header.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    MainWindow.ReleaseCapture();
                    MainWindow.SendMessage(Handle, 0xA1, 0x2, 0);
                }
            };

            Button btnClose = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 32),
                Location = new Point(ClientSize.Width - 44, 10),
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(196, 43, 28);
            btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(178, 37, 23);
            btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.White;
            btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Theme.TextSecondary;
            btnClose.Click += (s, e) => Close();
            header.Controls.Add(btnClose);

            // 2. Content Panel
            Panel content = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 20, 28, 20)
            };

            int curY = 16;

            // Refresh Interval
            Label lblInterval = new Label
            {
                Text = "🔄 自动刷新时间间隔：",
                Font = Theme.FontSubtitle,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, curY)
            };
            content.Controls.Add(lblInterval);

            _cboInterval = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = Theme.FontSubtitle,
                BackColor = Color.FromArgb(35, 35, 52),
                ForeColor = Theme.TextPrimary,
                FlatStyle = FlatStyle.Flat,
                Width = 140,
                Location = new Point(230, curY - 3)
            };
            _cboInterval.Items.Add("1 秒");
            _cboInterval.Items.Add("2 秒");
            _cboInterval.Items.Add("3 秒 (推荐)");
            _cboInterval.Items.Add("5 秒");
            _cboInterval.Items.Add("10 秒");
            _cboInterval.Items.Add("30 秒");

            switch (_config.RefreshIntervalSeconds)
            {
                case 1: _cboInterval.SelectedIndex = 0; break;
                case 2: _cboInterval.SelectedIndex = 1; break;
                case 3: _cboInterval.SelectedIndex = 2; break;
                case 5: _cboInterval.SelectedIndex = 3; break;
                case 10: _cboInterval.SelectedIndex = 4; break;
                case 30: _cboInterval.SelectedIndex = 5; break;
                default: _cboInterval.SelectedIndex = 2; break;
            }
            content.Controls.Add(_cboInterval);

            curY += 46;

            // Auto Start
            _chkAutoStart = new CheckBox
            {
                Text = "🚀 开机时自动在后台启动",
                Font = Theme.FontSubtitle,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, curY),
                Checked = _config.StartWithWindows,
                Cursor = Cursors.Hand
            };
            content.Controls.Add(_chkAutoStart);

            curY += 40;

            // Minimize to tray
            _chkMinToTray = new CheckBox
            {
                Text = "📦 关闭主窗口时最小化到系统托盘",
                Font = Theme.FontSubtitle,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, curY),
                Checked = _config.MinimizeToTray,
                Cursor = Cursors.Hand
            };
            content.Controls.Add(_chkMinToTray);

            curY += 40;

            // Show floating widget
            _chkFloatingWidget = new CheckBox
            {
                Text = "🖥️ 显示桌面半透明极简电量悬浮窗",
                Font = Theme.FontSubtitle,
                ForeColor = Theme.TextPrimary,
                AutoSize = true,
                Location = new Point(28, curY),
                Checked = _config.ShowFloatingWidget,
                Cursor = Cursors.Hand
            };
            content.Controls.Add(_chkFloatingWidget);

            curY += 60;

            // Save Button
            Button btnSave = new Button
            {
                Text = "✔ 保存设置",
                Font = Theme.FontDeviceName,
                ForeColor = Color.White,
                BackColor = Theme.Accent,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(160, 40),
                Location = new Point(48, curY),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) => SaveSettings();
            content.Controls.Add(btnSave);

            // Cancel Button
            Button btnCancel = new Button
            {
                Text = "取消",
                Font = Theme.FontDeviceName,
                ForeColor = Theme.TextSecondary,
                BackColor = Color.FromArgb(40, 40, 60),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(120, 40),
                Location = new Point(228, curY),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => Close();
            content.Controls.Add(btnCancel);

            curY += 52;

            Label lblVersion = new Label
            {
                Text = "OmniBattery  •  v1.0.0",
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Size = new Size(364, 20),
                Location = new Point(28, curY)
            };
            content.Controls.Add(lblVersion);

            Controls.Add(content);
            Controls.Add(header);

            Paint += (s, e) =>
            {
                using (var pen = new Pen(Theme.CardBorder, 1.5f))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
                }
            };
        }

        private void SaveSettings()
        {
            switch (_cboInterval.SelectedIndex)
            {
                case 0: _config.RefreshIntervalSeconds = 1; break;
                case 1: _config.RefreshIntervalSeconds = 2; break;
                case 2: _config.RefreshIntervalSeconds = 3; break;
                case 3: _config.RefreshIntervalSeconds = 5; break;
                case 4: _config.RefreshIntervalSeconds = 10; break;
                case 5: _config.RefreshIntervalSeconds = 30; break;
                default: _config.RefreshIntervalSeconds = 3; break;
            }

            _config.StartWithWindows = _chkAutoStart.Checked;
            _config.MinimizeToTray = _chkMinToTray.Checked;
            _config.ShowFloatingWidget = _chkFloatingWidget.Checked;

            ConfigService.Save(_config);
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
