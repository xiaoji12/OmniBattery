using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using EasyBluetooth.Models;
using EasyBluetooth.Scanners;
using EasyBluetooth.Services;

namespace EasyBluetooth.UI
{
    public sealed class MainWindow : Form
    {
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string lpString);

        public static readonly int WM_SHOWME = RegisterWindowMessage("OmniBattery_Show_MainWindow_Message");

        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 0x2;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SHOWME)
            {
                ShowMainWindow();
                return;
            }
            base.WndProc(ref m);
        }

        public void ShowMainWindow()
        {
            Show();
            WindowState = FormWindowState.Normal;
            BringToFront();
            Activate();
        }

        private readonly AppConfig _config;
        private readonly RtssExporter _rtssExporter;
        private FloatingWidgetWindow _widgetWindow;
        private TrayManager _trayManager;

        private System.Windows.Forms.Timer _scanTimer;
        private bool _isScanning;
        private bool _reallyExit;

        private Panel _titleBar;
        private FlowLayoutPanel _deviceContainer;
        private Panel _emptyPanel;
        private Label _versionLabel;

        private List<DeviceInfo> _currentDevices = new List<DeviceInfo>();

        public MainWindow()
        {
            _config = ConfigService.Load();
            _rtssExporter = new RtssExporter();

            InitializeForm();
            InitializeTrayAndWidget();
            StartTimer();

            // Perform initial scan
            PerformScan();
        }

        private void InitializeForm()
        {
            Text = "OmniBattery";
            Icon = TrayManager.CreateAppIcon();
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(840, 560);
            MinimumSize = new Size(720, 480);
            FormBorderStyle = FormBorderStyle.None;
            BackColor = Theme.BgDark;
            DoubleBuffered = true;

            // 1. Title bar (WinUI 3 50px height)
            _titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Theme.BgHeader
            };
            _titleBar.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    ReleaseCapture();
                    SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
                }
            };
            _titleBar.Paint += TitleBar_Paint;

            // Title Buttons (WinUI 3 Pill & Control buttons)
            int btnY = 10;
            var btnClose = CreateTitleButton("✕", 0, btnY, (s, e) => Close(), true);
            var btnMin = CreateTitleButton("—", 0, btnY, (s, e) => WindowState = FormWindowState.Minimized, false);
            var btnSettings = CreateActionButton("⚙️ 设置", 0, btnY, 74, (s, e) => OpenSettings());
            var btnWidget = CreateActionButton("📌 悬浮窗", 0, btnY, 84, (s, e) => ToggleWidget());
            var btnRefresh = CreateActionButton("🔄 刷新", 0, btnY, 72, (s, e) => PerformScan());

            _titleBar.Controls.Add(btnClose);
            _titleBar.Controls.Add(btnMin);
            _titleBar.Controls.Add(btnSettings);
            _titleBar.Controls.Add(btnWidget);
            _titleBar.Controls.Add(btnRefresh);

            Action repositionButtons = () =>
            {
                int rX = _titleBar.Width - 12;
                rX -= 40;
                btnClose.Location = new Point(rX, btnY);
                rX -= 38;
                btnMin.Location = new Point(rX, btnY);
                rX -= 12;
                rX -= 74;
                btnSettings.Location = new Point(rX, btnY);
                rX -= 6;
                rX -= 84;
                btnWidget.Location = new Point(rX, btnY);
                rX -= 6;
                rX -= 72;
                btnRefresh.Location = new Point(rX, btnY);
            };

            _titleBar.Resize += (s, e) => repositionButtons();
            repositionButtons();

            // 2. Device Container
            _deviceContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(24, 20, 24, 20),
                BackColor = Theme.BgDark
            };
            EnableDoubleBuffering(_deviceContainer);
            _deviceContainer.Resize += (s, e) => UpdateCardWidths();

            // 3. Empty State Panel
            _emptyPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Theme.BgDark,
                Visible = false
            };
            _emptyPanel.Paint += EmptyPanel_Paint;

            // 4. Version Label in Bottom Right Corner
            _versionLabel = new Label
            {
                Text = "v1.0.0",
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                ForeColor = Color.FromArgb(100, 116, 139),
                BackColor = Theme.BgDark,
                AutoSize = true,
                Cursor = Cursors.Default
            };

            Controls.Add(_versionLabel);
            Controls.Add(_deviceContainer);
            Controls.Add(_emptyPanel);
            Controls.Add(_titleBar);

            _versionLabel.BringToFront();

            Resize += (s, e) =>
            {
                if (_versionLabel != null)
                {
                    _versionLabel.Location = new Point(ClientSize.Width - _versionLabel.Width - 16, ClientSize.Height - _versionLabel.Height - 10);
                    _versionLabel.BringToFront();
                }
            };
            Shown += (s, e) =>
            {
                if (_versionLabel != null)
                {
                    _versionLabel.Location = new Point(ClientSize.Width - _versionLabel.Width - 16, ClientSize.Height - _versionLabel.Height - 10);
                    _versionLabel.BringToFront();
                }
            };

            FormClosing += MainWindow_FormClosing;
        }

        private static void EnableDoubleBuffering(Control c)
        {
            try
            {
                typeof(Control).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.SetProperty |
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.NonPublic,
                    null, c, new object[] { true });
            }
            catch { }
        }

        private void InitializeTrayAndWidget()
        {
            _widgetWindow = new FloatingWidgetWindow(() =>
            {
                Show();
                WindowState = FormWindowState.Normal;
                BringToFront();
            });

            if (_config.ShowFloatingWidget)
            {
                _widgetWindow.Location = new Point(_config.WidgetX, _config.WidgetY);
                _widgetWindow.Show();
            }

            _trayManager = new TrayManager(
                _config,
                onOpenMain: () =>
                {
                    Show();
                    WindowState = FormWindowState.Normal;
                    BringToFront();
                },
                onToggleWidget: ToggleWidget,
                onRefresh: PerformScan,
                onExit: ExitApp
            );
        }

        private void StartTimer()
        {
            _scanTimer = new System.Windows.Forms.Timer
            {
                Interval = Math.Max(1000, _config.RefreshIntervalSeconds * 1000)
            };
            _scanTimer.Tick += (s, e) => PerformScan();
            _scanTimer.Start();
        }

        private void OpenSettings()
        {
            using (var dlg = new SettingsDialog(_config))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    if (_scanTimer != null)
                    {
                        _scanTimer.Interval = Math.Max(1000, _config.RefreshIntervalSeconds * 1000);
                    }
                    if (_widgetWindow != null)
                    {
                        if (_config.ShowFloatingWidget && !_widgetWindow.Visible)
                        {
                            _widgetWindow.Location = new Point(_config.WidgetX, _config.WidgetY);
                            _widgetWindow.Show();
                        }
                        else if (!_config.ShowFloatingWidget && _widgetWindow.Visible)
                        {
                            _widgetWindow.Hide();
                        }
                    }
                    if (_trayManager != null)
                    {
                        _trayManager.UpdateWidgetChecked(_config.ShowFloatingWidget);
                    }
                    PerformScan();
                }
            }
        }

        private void PerformScan()
        {
            if (_isScanning) return;
            _isScanning = true;

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    var scanned = DeviceScanner.ScanAll();
                    _currentDevices = scanned;

                    // 1. Exporters (Data services run in background)
                    try
                    {
                        if (_config.EnableAida64)
                        {
                            Aida64Exporter.Export(scanned);
                        }
                        if (_config.EnableRtss)
                        {
                            _rtssExporter.Export(scanned);
                        }
                    }
                    catch { }

                    // 2. Dispatch to UI thread smoothly
                    if (IsDisposed || !IsHandleCreated) return;

                    BeginInvoke(new Action(() =>
                    {
                        if (IsDisposed) return;
                        try
                        {
                            if (_trayManager != null) _trayManager.UpdateDevices(scanned);
                            if (_widgetWindow != null) _widgetWindow.UpdateDevices(scanned);
                            UpdateDeviceList(scanned);
                        }
                        catch { }
                    }));
                }
                catch { }
                finally
                {
                    _isScanning = false;
                }
            });
        }

        private void UpdateDeviceList(List<DeviceInfo> devices)
        {
            if (devices == null || devices.Count == 0)
            {
                _emptyPanel.Visible = true;
                _deviceContainer.Visible = false;
                _deviceContainer.Controls.Clear();
                return;
            }

            _emptyPanel.Visible = false;
            _deviceContainer.Visible = true;

            int targetWidth = _deviceContainer.ClientSize.Width - 60;
            if (targetWidth < 300) targetWidth = 300;

            // Check if existing controls match device ids to avoid re-creating and UI flickering
            bool canReuse = false;
            if (_deviceContainer.Controls.Count == devices.Count)
            {
                canReuse = true;
                for (int i = 0; i < devices.Count; i++)
                {
                    var card = _deviceContainer.Controls[i] as DeviceCardControl;
                    if (card == null || card.Device == null || card.Device.Id != devices[i].Id)
                    {
                        canReuse = false;
                        break;
                    }
                }
            }

            if (canReuse)
            {
                // In-place refresh without flickering
                for (int i = 0; i < devices.Count; i++)
                {
                    var card = (DeviceCardControl)_deviceContainer.Controls[i];
                    card.Width = targetWidth;
                    card.Device = devices[i]; // will trigger Invalidate() smoothly
                }
            }
            else
            {
                _deviceContainer.SuspendLayout();
                _deviceContainer.Controls.Clear();

                foreach (var d in devices)
                {
                    var card = new DeviceCardControl(d, updatedDev =>
                    {
                        try
                        {
                            if (_config.EnableAida64) Aida64Exporter.Export(_currentDevices);
                            if (_config.EnableRtss) _rtssExporter.Export(_currentDevices);
                            if (_trayManager != null) _trayManager.UpdateDevices(_currentDevices);
                            if (_widgetWindow != null) _widgetWindow.UpdateDevices(_currentDevices);
                        }
                        catch { }
                    })
                    {
                        Width = targetWidth,
                        Margin = new Padding(0, 0, 0, 14)
                    };
                    _deviceContainer.Controls.Add(card);
                }

                _deviceContainer.ResumeLayout();
            }
        }

        private void UpdateCardWidths()
        {
            int targetWidth = _deviceContainer.ClientSize.Width - 60;
            if (targetWidth < 300) targetWidth = 300;

            foreach (Control c in _deviceContainer.Controls)
            {
                c.Width = targetWidth;
            }
        }

        private void ToggleWidget()
        {
            _config.ShowFloatingWidget = !_config.ShowFloatingWidget;
            if (_config.ShowFloatingWidget)
            {
                _widgetWindow.Location = new Point(_config.WidgetX, _config.WidgetY);
                _widgetWindow.Show();
            }
            else
            {
                _widgetWindow.Hide();
            }
            _trayManager.UpdateWidgetChecked(_config.ShowFloatingWidget);
            ConfigService.Save(_config);
        }

        private void ExitApp()
        {
            _reallyExit = true;
            _scanTimer.Stop();
            _widgetWindow.Close();
            _trayManager.Dispose();
            _rtssExporter.Dispose();
            Close();
            Application.Exit();
        }

        private void MainWindow_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!_reallyExit && _config.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                _scanTimer.Stop();
                _widgetWindow.Close();
                _trayManager.Dispose();
                _rtssExporter.Dispose();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // 1px WinUI 3 Subtle Window Border
            using (var pen = new Pen(Theme.CardBorder, 1f))
            {
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            }
        }

        private void TitleBar_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // WinUI 3 App Icon Container
            int iconX = 18;
            int iconY = 11;
            int iconSize = 28;
            Rectangle iconBox = new Rectangle(iconX, iconY, iconSize, iconSize);
            using (var path = Theme.CreateRoundedRectangle(iconBox, 6))
            {
                using (var brush = new LinearGradientBrush(
                    iconBox,
                    Theme.Accent,
                    Theme.AccentPurple,
                    LinearGradientMode.ForwardDiagonal))
                {
                    g.FillPath(brush, path);
                }
            }

            // High-precision Lightning glyph
            Point[] pts = new Point[]
            {
                new Point(iconX + 15, iconY + 5),
                new Point(iconX + 9, iconY + 15),
                new Point(iconX + 14, iconY + 15),
                new Point(iconX + 13, iconY + 23),
                new Point(iconX + 20, iconY + 13),
                new Point(iconX + 15, iconY + 13)
            };
            using (var brush = new SolidBrush(Color.White))
            {
                g.FillPolygon(brush, pts);
            }

            // Title: ONLY "OmniBattery" (Clean WinUI 3 Title, all extra trailing words removed)
            using (var brush = new SolidBrush(Theme.TextPrimary))
            {
                g.DrawString("OmniBattery", Theme.FontTitle, brush, iconX + 36, iconY + 3);
            }

            // WinUI 3 Subtle Header Line
            using (var pen = new Pen(Theme.HeaderBorder, 1f))
            {
                g.DrawLine(pen, 0, _titleBar.Height - 1, _titleBar.Width, _titleBar.Height - 1);
            }
        }

        private void EmptyPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            StringFormat sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            Rectangle rect = _emptyPanel.ClientRectangle;

            using (var f = new Font("Segoe UI Emoji", 38f))
            {
                using (var brush = new SolidBrush(Color.FromArgb(90, 255, 255, 255)))
                {
                    g.DrawString("🔋", f, brush, new Rectangle(rect.X, rect.Y - 50, rect.Width, 100), sf);
                }
            }

            using (var brush = new SolidBrush(Theme.TextSecondary))
            {
                g.DrawString("当前未检测到已连接的无线/蓝牙设备\n连接设备后将自动显示（已严格过滤未连接设备）", Theme.FontDeviceName, brush, new Rectangle(rect.X, rect.Y + 40, rect.Width, 80), sf);
            }
        }

        private Button CreateTitleButton(string text, int x, int y, EventHandler onClick, bool isClose = false)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(36, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Theme.TextSecondary,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            if (isClose)
            {
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(196, 43, 28); // WinUI 3 Close Red
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(178, 37, 23);
                btn.MouseEnter += (s, e) => btn.ForeColor = Color.White;
                btn.MouseLeave += (s, e) => btn.ForeColor = Theme.TextSecondary;
            }
            else
            {
                btn.FlatAppearance.MouseOverBackColor = Theme.CardHover;
                btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(40, 40, 40);
            }
            btn.Click += onClick;
            return btn;
        }

        private Button CreateActionButton(string text, int x, int y, int width, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 30),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Theme.TextPrimary,
                BackColor = Theme.CardBg,
                Font = Theme.FontSubtitle,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Theme.CardBorder;
            btn.FlatAppearance.MouseOverBackColor = Theme.CardHover;
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(36, 36, 36);
            btn.Click += onClick;
            return btn;
        }
    }
}
