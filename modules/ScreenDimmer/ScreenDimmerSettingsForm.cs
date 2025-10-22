using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace TinyTools.Modules.ScreenDimmer
{
    // Extension methods for rounded rectangles
    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using (var path = GetRoundedRectPath(rect, radius))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle rect, int radius)
        {
            using (var path = GetRoundedRectPath(rect, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        private static GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            
            return path;
        }
    }

    public partial class ScreenDimmerSettingsForm : Form
    {
        private readonly ScreenDimmerManager manager;
        
        // Controls
        private TrackBar brightnessSlider = null!;
        private Label brightnessLabel = null!;
        private Label methodLabel = null!;
        private ComboBox methodComboBox = null!;
        private Label statusLabel = null!;
        private Button resetButton = null!;
        private Button closeButton = null!;
        private GroupBox brightnessGroup = null!;
        private GroupBox methodGroup = null!;
        private GroupBox hotkeyGroup = null!;
        private GroupBox actionsGroup = null!;
        private GroupBox monitorsGroup = null!;
        private CheckBox globalHotkeyCheckBox = null!;
        private Panel monitorsPanel = null!;
        
        // Monitor selection
        private List<ScreenDimmerManager.MonitorInfo> monitors = new List<ScreenDimmerManager.MonitorInfo>();
        private List<MonitorBox> monitorBoxes = new List<MonitorBox>();

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

        // Monitor enumeration P/Invoke
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFOEX
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string szDevice;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }


        public class MonitorBox : Panel
        {
            public ScreenDimmerManager.MonitorInfo Monitor { get; set; }
            public bool IsSelected { get; set; } = true;
            private bool isHovered = false;

            public MonitorBox(ScreenDimmerManager.MonitorInfo monitor)
            {
                Monitor = monitor;
                SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer | ControlStyles.ResizeRedraw, true);
                Cursor = Cursors.Hand;
            }

            protected override void OnMouseEnter(EventArgs e)
            {
                isHovered = true;
                Invalidate();
                base.OnMouseEnter(e);
            }

            protected override void OnMouseLeave(EventArgs e)
            {
                isHovered = false;
                Invalidate();
                base.OnMouseLeave(e);
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                var g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;

                // Calculate colors based on state
                Color baseColor = IsSelected ? Color.FromArgb(0, 120, 215) : Color.FromArgb(60, 60, 60);
                Color borderColor = IsSelected ? Color.FromArgb(0, 100, 180) : Color.FromArgb(100, 100, 100);
                Color textColor = IsSelected ? Color.White : Color.LightGray;

                if (isHovered)
                {
                    baseColor = IsSelected ? Color.FromArgb(20, 140, 235) : Color.FromArgb(80, 80, 80);
                    borderColor = IsSelected ? Color.FromArgb(20, 120, 200) : Color.FromArgb(120, 120, 120);
                }

                // Draw background with gradient
                using (var brush = new LinearGradientBrush(ClientRectangle, baseColor, Color.FromArgb(baseColor.A, Math.Max(0, baseColor.R - 20), Math.Max(0, baseColor.G - 20), Math.Max(0, baseColor.B - 20)), 90f))
                {
                    g.FillRoundedRectangle(brush, ClientRectangle, 8);
                }

                // Draw border
                using (var pen = new Pen(borderColor, 2))
                {
                    g.DrawRoundedRectangle(pen, new Rectangle(1, 1, Width - 2, Height - 2), 8);
                }

                // Draw monitor icon
                var iconRect = new Rectangle(10, 10, 32, 24);
                using (var iconBrush = new SolidBrush(textColor))
                {
                    g.FillRoundedRectangle(iconBrush, iconRect, 3);
                    g.FillRectangle(iconBrush, new Rectangle(iconRect.X + 12, iconRect.Bottom, 8, 4));
                    g.FillRectangle(iconBrush, new Rectangle(iconRect.X + 8, iconRect.Bottom + 4, 16, 2));
                }

                // Draw primary indicator
                if (Monitor.IsPrimary)
                {
                    using (var starBrush = new SolidBrush(Color.Gold))
                    {
                        var starRect = new Rectangle(Width - 25, 5, 15, 15);
                        DrawStar(g, starBrush, starRect);
                    }
                }

                // Draw text
                var font = new Font("Segoe UI", 9f, FontStyle.Bold);
                var textBrush = new SolidBrush(textColor);
                
                string displayText = Monitor.IsPrimary ? "Primary" : "Monitor";
                var textRect = new Rectangle(10, 40, Width - 20, 20);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(displayText, font, textBrush, textRect, sf);

                // Draw resolution
                var resFont = new Font("Segoe UI", 8f);
                string resolution = $"{Monitor.Bounds.Width}×{Monitor.Bounds.Height}";
                var resRect = new Rectangle(10, 60, Width - 20, 15);
                g.DrawString(resolution, resFont, textBrush, resRect, sf);

                font.Dispose();
                resFont.Dispose();
                textBrush.Dispose();
            }

            private void DrawStar(Graphics g, Brush brush, Rectangle rect)
            {
                var points = new PointF[10];
                var center = new PointF(rect.X + rect.Width / 2f, rect.Y + rect.Height / 2f);
                var outerRadius = Math.Min(rect.Width, rect.Height) / 2f;
                var innerRadius = outerRadius * 0.4f;

                for (int i = 0; i < 10; i++)
                {
                    var angle = i * Math.PI / 5 - Math.PI / 2;
                    var radius = (i % 2 == 0) ? outerRadius : innerRadius;
                    points[i] = new PointF(
                        center.X + (float)(Math.Cos(angle) * radius),
                        center.Y + (float)(Math.Sin(angle) * radius)
                    );
                }

                g.FillPolygon(brush, points);
            }
        }

        public ScreenDimmerSettingsForm(ScreenDimmerManager manager)
        {
            this.manager = manager;
            DetectMonitors();
            InitializeComponent();
            LoadCurrentSettings();
            
            // Subscribe to manager events
            manager.BrightnessChanged += OnBrightnessChanged;
            manager.MethodChanged += OnMethodChanged;
        }

        private void DetectMonitors()
        {
            monitors.Clear();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);
        }

        private bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            MONITORINFOEX mi = new MONITORINFOEX();
            mi.cbSize = Marshal.SizeOf(mi);
            
            if (GetMonitorInfo(hMonitor, ref mi))
            {
                var monitor = new ScreenDimmerManager.MonitorInfo
                {
                    Handle = hMonitor,
                    Bounds = new Rectangle(mi.rcMonitor.Left, mi.rcMonitor.Top, 
                                         mi.rcMonitor.Right - mi.rcMonitor.Left, 
                                         mi.rcMonitor.Bottom - mi.rcMonitor.Top),
                    WorkArea = new Rectangle(mi.rcWork.Left, mi.rcWork.Top,
                                           mi.rcWork.Right - mi.rcWork.Left,
                                           mi.rcWork.Bottom - mi.rcWork.Top),
                    DeviceName = mi.szDevice,
                    IsPrimary = (mi.dwFlags & 1) != 0,
                    IsSelected = true
                };
                
                monitors.Add(monitor);
            }
            
            return true;
        }

        private static Icon GetEmbeddedScreenDimmerIcon()
        {
            // Use the embedded ScreenDimmer-specific icon
            return ScreenDimmerResources.GetScreenDimmerIcon();
        }

        private void InitializeComponent()
        {
            // Form properties
            this.Text = "Screen Dimmer Settings";
            this.Size = new Size(450, 580);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = true;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.ShowInTaskbar = true;

            try
            {
                // Use embedded icon from executable resources
                this.Icon = GetEmbeddedScreenDimmerIcon();
            }
            catch
            {
                // Use default icon if loading fails
                this.Icon = SystemIcons.Application;
            }

            // Brightness Group
            brightnessGroup = new GroupBox
            {
                Text = "Brightness Control",
                Location = new Point(20, 20),
                Size = new Size(400, 120),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            // Enable mouse wheel scrolling for brightness control
            brightnessGroup.MouseWheel += BrightnessGroup_MouseWheel;
            brightnessGroup.MouseEnter += (s, e) => brightnessGroup.Focus();
            this.Controls.Add(brightnessGroup);

            // Brightness label
            brightnessLabel = new Label
            {
                Text = "Brightness: 100%",
                Location = new Point(20, 30),
                Size = new Size(360, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F)
            };
            brightnessLabel.MouseWheel += BrightnessGroup_MouseWheel;
            brightnessGroup.Controls.Add(brightnessLabel);

            // Brightness slider
            brightnessSlider = new TrackBar
            {
                Location = new Point(20, 60),
                Size = new Size(360, 45),
                Minimum = 1,
                Maximum = 100,
                Value = 100,
                TickFrequency = 10,
                TickStyle = TickStyle.BottomRight
            };
            brightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;
            brightnessSlider.MouseWheel += BrightnessGroup_MouseWheel;
            brightnessGroup.Controls.Add(brightnessSlider);

            // Monitors Group
            monitorsGroup = new GroupBox
            {
                Text = "Monitor Selection",
                Location = new Point(20, 150),
                Size = new Size(400, 140),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(monitorsGroup);

            // Monitors panel
            monitorsPanel = new Panel
            {
                Location = new Point(10, 25),
                Size = new Size(380, 105),
                AutoScroll = true
            };
            monitorsGroup.Controls.Add(monitorsPanel);

            CreateMonitorBoxes();

            // Method Group
            methodGroup = new GroupBox
            {
                Text = "Dimming Method",
                Location = new Point(20, 300),
                Size = new Size(400, 100),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(methodGroup);

            // Method label
            methodLabel = new Label
            {
                Text = "Method:",
                Location = new Point(20, 30),
                Size = new Size(80, 20),
                Font = new Font("Segoe UI", 9F)
            };
            methodGroup.Controls.Add(methodLabel);

            // Method combo box
            methodComboBox = new ComboBox
            {
                Location = new Point(110, 28),
                Size = new Size(270, 23),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9F)
            };
            methodComboBox.Items.AddRange(new string[]
            {
                "Hardware Gamma Ramp",
                "Software Overlay",
                "Auto (Try Gamma First)"
            });
            methodComboBox.SelectedIndex = 2; // Auto mode
            methodComboBox.SelectedIndexChanged += MethodComboBox_SelectedIndexChanged;
            methodGroup.Controls.Add(methodComboBox);

            // Status label
            statusLabel = new Label
            {
                Text = "Status: Ready",
                Location = new Point(20, 60),
                Size = new Size(360, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 8F),
                ForeColor = Color.DarkGreen
            };
            methodGroup.Controls.Add(statusLabel);

            // Hotkey Group
            hotkeyGroup = new GroupBox
            {
                Text = "Global Hotkey",
                Location = new Point(20, 410),
                Size = new Size(400, 60),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(hotkeyGroup);

            // Global hotkey checkbox
            globalHotkeyCheckBox = new CheckBox
            {
                Text = "Enable Ctrl+Shift+Mouse Wheel brightness control",
                Location = new Point(20, 25),
                Size = new Size(360, 25),
                Font = new Font("Segoe UI", 9F),
                Checked = true
            };
            globalHotkeyCheckBox.CheckedChanged += GlobalHotkeyCheckBox_CheckedChanged;
            hotkeyGroup.Controls.Add(globalHotkeyCheckBox);

            // Actions Group
            actionsGroup = new GroupBox
            {
                Text = "Actions",
                Location = new Point(20, 480),
                Size = new Size(400, 60),
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            this.Controls.Add(actionsGroup);

            // Reset button
            resetButton = new Button
            {
                Text = "Reset to 100%",
                Location = new Point(20, 25),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 9F)
            };
            resetButton.Click += ResetButton_Click;
            actionsGroup.Controls.Add(resetButton);

            // Close button
            closeButton = new Button
            {
                Text = "Close",
                Location = new Point(300, 25),
                Size = new Size(80, 25),
                Font = new Font("Segoe UI", 9F)
            };
            closeButton.Click += CloseButton_Click;
            actionsGroup.Controls.Add(closeButton);
        }

        private void CreateMonitorBoxes()
        {
            monitorBoxes.Clear();
            monitorsPanel.Controls.Clear();

            int boxWidth = 100;
            int boxHeight = 80;
            int spacing = 10;
            int x = 10;

            foreach (var monitor in monitors)
            {
                var monitorBox = new MonitorBox(monitor)
                {
                    Location = new Point(x, 10),
                    Size = new Size(boxWidth, boxHeight),
                    IsSelected = monitor.IsSelected
                };

                monitorBox.Click += MonitorBox_Click;
                monitorBoxes.Add(monitorBox);
                monitorsPanel.Controls.Add(monitorBox);

                x += boxWidth + spacing;
            }
        }

        private void MonitorBox_Click(object? sender, EventArgs e)
        {
            if (sender is MonitorBox monitorBox)
            {
                // Toggle selection
                monitorBox.IsSelected = !monitorBox.IsSelected;
                monitorBox.Monitor.IsSelected = monitorBox.IsSelected;
                monitorBox.Invalidate();

                // Update manager with selected monitors
                manager.UpdateSelectedMonitors(monitors.Where(m => m.IsSelected).ToList());

                // If monitor was deselected, reset its brightness to 100%
                if (!monitorBox.IsSelected)
                {
                    manager.ResetMonitorBrightness(monitorBox.Monitor);
                }
            }
        }

        private void LoadCurrentSettings()
        {
            // Load brightness
            brightnessSlider.Value = manager.Brightness;
            UpdateBrightnessLabel(manager.Brightness);

            // Load method
            methodComboBox.SelectedIndex = (int)manager.Method;
            
            // Load global hotkey setting
            globalHotkeyCheckBox.Checked = manager.GlobalHotkeyEnabled;
            
            UpdateStatusLabel();
        }

        private void UpdateBrightnessLabel(int brightness)
        {
            brightnessLabel.Text = $"Brightness: {brightness}%";
        }

        private void UpdateStatusLabel()
        {
            var method = manager.Method;
            string methodText = method switch
            {
                ScreenDimmerManager.DimmingMethod.GammaRamp => "Hardware Gamma Ramp",
                ScreenDimmerManager.DimmingMethod.Overlay => "Software Overlay",
                ScreenDimmerManager.DimmingMethod.Auto => "Auto Mode",
                _ => "Unknown"
            };
            
            statusLabel.Text = $"Status: Active - {methodText}";
            statusLabel.ForeColor = Color.DarkGreen;
        }

        // Event handlers
        private void BrightnessSlider_ValueChanged(object? sender, EventArgs e)
        {
            int brightness = brightnessSlider.Value;
            manager.Brightness = brightness;
            UpdateBrightnessLabel(brightness);
        }

        private void MethodComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            var method = (ScreenDimmerManager.DimmingMethod)methodComboBox.SelectedIndex;
            manager.Method = method;
            UpdateStatusLabel();
        }

        private void ResetButton_Click(object? sender, EventArgs e)
        {
            brightnessSlider.Value = 100;
            manager.Brightness = 100;
            UpdateBrightnessLabel(100);
        }

        private void CloseButton_Click(object? sender, EventArgs e)
        {
            this.Hide();
        }

        private void GlobalHotkeyCheckBox_CheckedChanged(object? sender, EventArgs e)
        {
            manager.GlobalHotkeyEnabled = globalHotkeyCheckBox.Checked;
        }

        private void BrightnessGroup_MouseWheel(object? sender, MouseEventArgs e)
        {
            // Calculate brightness change based on wheel delta
            // Standard wheel delta is 120 per notch, we'll change by 1% per notch
            int brightnessChange = (e.Delta / 120) * 1;
            int newBrightness = Math.Max(1, Math.Min(100, brightnessSlider.Value + brightnessChange));
            
            if (newBrightness != brightnessSlider.Value)
            {
                brightnessSlider.Value = newBrightness;
                manager.Brightness = newBrightness;
                UpdateBrightnessLabel(newBrightness);
            }
        }

        // Manager event handlers
        private void OnBrightnessChanged(object? sender, int brightness)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnBrightnessChanged(sender, brightness)));
                return;
            }

            if (brightnessSlider.Value != brightness)
            {
                brightnessSlider.Value = brightness;
            }
            UpdateBrightnessLabel(brightness);
        }

        private void OnMethodChanged(object? sender, ScreenDimmerManager.DimmingMethod method)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnMethodChanged(sender, method)));
                return;
            }

            if (methodComboBox.SelectedIndex != (int)method)
            {
                methodComboBox.SelectedIndex = (int)method;
            }
            UpdateStatusLabel();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Hide instead of closing to keep the form available
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
            else
            {
                // Unsubscribe from events
                manager.BrightnessChanged -= OnBrightnessChanged;
                manager.MethodChanged -= OnMethodChanged;
                base.OnFormClosing(e);
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Handle ESC key to close the window
            if (keyData == Keys.Escape)
            {
                this.Hide();
                return true;
            }
            
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            if (value)
            {
                LoadCurrentSettings();
            }
        }
    }
}
