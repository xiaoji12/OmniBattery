using System;
using System.Drawing;
using System.Windows.Forms;
using EasyBluetooth.Models;
using EasyBluetooth.Services;

namespace EasyBluetooth.UI
{
    public sealed class BatteryCalibrationDialog : Form
    {
        private readonly DeviceInfo _device;
        private TrackBar _slider;
        private Label _lblPercent;
        private CheckBox _chkCharging;

        public BatteryCalibrationDialog(DeviceInfo device)
        {
            _device = device;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "电量校准与设置 - " + _device.DisplayName;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(420, 260);
            BackColor = Theme.BgDark;
            ForeColor = Theme.TextPrimary;
            Font = Theme.FontSubtitle;

            // Title label
            var lblTitle = new Label
            {
                Text = string.Format("🔋 {0} ({1})", _device.DisplayName, _device.ConnectionType),
                Location = new Point(24, 18),
                AutoSize = true,
                Font = Theme.FontDeviceName,
                ForeColor = Theme.TextPrimary
            };
            Controls.Add(lblTitle);

            var lblDesc = new Label
            {
                Text = "可根据设备实际电量进行手动校准，设置将实时同步至悬浮窗与 AIDA64/RTSS",
                Location = new Point(24, 46),
                Size = new Size(372, 34),
                Font = Theme.FontSmall,
                ForeColor = Theme.TextMuted
            };
            Controls.Add(lblDesc);

            int curBat = _device.Battery.HasValue ? _device.Battery.Value : 90;

            // Big percent label
            _lblPercent = new Label
            {
                Text = curBat + "%",
                Location = new Point(24, 88),
                Size = new Size(90, 36),
                Font = new Font("Microsoft YaHei UI", 18f, FontStyle.Bold),
                ForeColor = Theme.GetBatteryColor(curBat, _device.IsCharging),
                TextAlign = ContentAlignment.MiddleLeft
            };
            Controls.Add(_lblPercent);

            // Slider
            _slider = new TrackBar
            {
                Minimum = 1,
                Maximum = 100,
                Value = curBat,
                TickFrequency = 10,
                Location = new Point(120, 88),
                Size = new Size(276, 45)
            };
            _slider.ValueChanged += (s, e) =>
            {
                _lblPercent.Text = _slider.Value + "%";
                _lblPercent.ForeColor = Theme.GetBatteryColor(_slider.Value, _chkCharging.Checked);
            };
            Controls.Add(_slider);

            // Preset buttons
            int[] presets = new int[] { 20, 50, 80, 100 };
            int btnX = 120;
            foreach (int p in presets)
            {
                int val = p;
                var btn = new Button
                {
                    Text = val + "%",
                    Location = new Point(btnX, 134),
                    Size = new Size(62, 26),
                    FlatStyle = FlatStyle.Flat,
                    BackColor = Color.FromArgb(36, 36, 54),
                    ForeColor = Theme.TextSecondary,
                    Font = Theme.FontSmall,
                    Cursor = Cursors.Hand
                };
                btn.FlatAppearance.BorderColor = Theme.CardBorder;
                btn.Click += (s, e) => _slider.Value = val;
                Controls.Add(btn);
                btnX += 70;
            }

            // Charging Checkbox
            _chkCharging = new CheckBox
            {
                Text = "⚡ 设备正在充电中",
                Location = new Point(24, 172),
                AutoSize = true,
                Checked = _device.IsCharging,
                Font = Theme.FontSubtitle,
                ForeColor = Theme.TextPrimary
            };
            _chkCharging.CheckedChanged += (s, e) =>
            {
                _lblPercent.ForeColor = Theme.GetBatteryColor(_slider.Value, _chkCharging.Checked);
            };
            Controls.Add(_chkCharging);

            // Save Button
            var btnSave = new Button
            {
                Text = "保存设置",
                Location = new Point(216, 212),
                Size = new Size(90, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Theme.Accent,
                ForeColor = Color.White,
                Font = Theme.FontSubtitle,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (s, e) =>
            {
                BatteryStore.SetBattery(_device.DisplayName, _slider.Value, _chkCharging.Checked);
                _device.Battery = _slider.Value;
                _device.IsCharging = _chkCharging.Checked;
                _device.LastUpdated = DateTime.Now;
                DialogResult = DialogResult.OK;
                Close();
            };
            Controls.Add(btnSave);

            // Cancel Button
            var btnCancel = new Button
            {
                Text = "取消",
                Location = new Point(316, 212),
                Size = new Size(80, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 36, 54),
                ForeColor = Theme.TextSecondary,
                Font = Theme.FontSubtitle,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Theme.CardBorder;
            btnCancel.Click += (s, e) =>
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            Controls.Add(btnCancel);
        }
    }
}
