using System;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace TinyTools.Modules.ScreenDimmer
{
    public partial class ScreenDimmerSettingsForm : Form
    {
        private readonly ScreenDimmerManager manager;
        
        // Controls
        private TrackBar brightnessSlider;
        private Label brightnessLabel;
        private Label methodLabel;
        private ComboBox methodComboBox;
        private Label statusLabel;
        private Button resetButton;
        private Button closeButton;
        private GroupBox brightnessGroup;
        private GroupBox methodGroup;
        private GroupBox actionsGroup;

        [DllImport("user32.dll")]
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        public ScreenDimmerSettingsForm(ScreenDimmerManager manager)
        {
            this.manager = manager;
            InitializeComponent();
            LoadCurrentSettings();
            
            // Subscribe to manager events
            manager.BrightnessChanged += OnBrightnessChanged;
            manager.MethodChanged += OnMethodChanged;
        }

        private static Icon GetEmbeddedScreenDimmerIcon()
        {
            try
            {
                // Try to load icon from embedded resources (resource ID 2)
                IntPtr hInstance = GetModuleHandle(null);
                IntPtr hIcon = LoadIcon(hInstance, new IntPtr(2));
                
                if (hIcon != IntPtr.Zero)
                {
                    return Icon.FromHandle(hIcon);
                }
            }
            catch
            {
                // Fallback to system icon if embedded icon fails
            }
            
            return SystemIcons.Application;
        }

        private void InitializeComponent()
        {
            // Form properties
            this.Text = "Screen Dimmer Settings";
            this.Size = new Size(450, 350);
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
            brightnessGroup.Controls.Add(brightnessLabel);

            // Brightness slider
            brightnessSlider = new TrackBar
            {
                Location = new Point(20, 60),
                Size = new Size(360, 45),
                Minimum = 10,
                Maximum = 100,
                Value = 100,
                TickFrequency = 10,
                TickStyle = TickStyle.BottomRight
            };
            brightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;
            brightnessGroup.Controls.Add(brightnessSlider);

            // Method Group
            methodGroup = new GroupBox
            {
                Text = "Dimming Method",
                Location = new Point(20, 150),
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

            // Actions Group
            actionsGroup = new GroupBox
            {
                Text = "Actions",
                Location = new Point(20, 260),
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

        private void LoadCurrentSettings()
        {
            // Load brightness
            brightnessSlider.Value = manager.Brightness;
            UpdateBrightnessLabel(manager.Brightness);

            // Load method
            methodComboBox.SelectedIndex = (int)manager.Method;
            
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
