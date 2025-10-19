using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScreenDimmer
{
    public partial class MainForm : Form
    {
        // P/Invoke declarations for gamma control
        [DllImport("gdi32.dll")]
        private static extern bool SetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport("gdi32.dll")]
        private static extern bool GetDeviceGammaRamp(IntPtr hDC, ref RAMP lpRamp);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        // Constants for hotkeys
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_ALT = 0x0001;
        private const uint VK_F1 = 0x70;
        private const int WM_HOTKEY = 0x0312;

        // Gamma ramp structure
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct RAMP
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Red;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Green;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
            public ushort[] Blue;
        }

        // Dimming methods
        public enum DimmingMethod
        {
            GammaRamp,
            Overlay,
            Auto
        }

        // Private fields
        private TrackBar brightnessSlider;
        private Label brightnessLabel;
        private Label methodLabel;
        private ComboBox methodComboBox;
        private Label statusLabel;
        private Button resetButton;
        private Button minimizeButton;
        private Button exitButton;
        private NotifyIcon trayIcon;
        private OverlayForm? overlayForm;

        private int currentBrightness = 100;
        private RAMP originalGammaRamp;
        private bool originalGammaStored = false;
        private DimmingMethod currentDimmingMethod = DimmingMethod.Auto;
        private DimmingMethod userSelectedMethod = DimmingMethod.Auto;
        private bool gammaSupported = true;
        private bool isMinimizedToTray = false;

        public MainForm()
        {
            InitializeComponent();
            InitializeApplication();
        }

        private void InitializeComponent()
        {
            // Form properties
            this.Text = "Screen Dimmer - Gamma Ramp Control";
            this.Size = new Size(420, 280);
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Icon = new Icon("favicon.ico");

            // Brightness label
            brightnessLabel = new Label
            {
                Text = "Brightness: 100%",
                Location = new Point(50, 20),
                Size = new Size(300, 25),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 10F)
            };
            this.Controls.Add(brightnessLabel);

            // Brightness slider
            brightnessSlider = new TrackBar
            {
                Location = new Point(50, 50),
                Size = new Size(300, 45),
                Minimum = 1,
                Maximum = 100,
                Value = 100,
                TickFrequency = 10,
                TickStyle = TickStyle.BottomRight
            };
            brightnessSlider.ValueChanged += BrightnessSlider_ValueChanged;
            this.Controls.Add(brightnessSlider);

            // Method label
            methodLabel = new Label
            {
                Text = "Dimming Method:",
                Location = new Point(50, 100),
                Size = new Size(120, 20),
                Font = new Font("Segoe UI", 9F)
            };
            this.Controls.Add(methodLabel);

            // Method combo box
            methodComboBox = new ComboBox
            {
                Location = new Point(180, 98),
                Size = new Size(170, 23),
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
            this.Controls.Add(methodComboBox);

            // Status label
            statusLabel = new Label
            {
                Text = "",
                Location = new Point(50, 130),
                Size = new Size(300, 20),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8F)
            };
            this.Controls.Add(statusLabel);

            // Reset button
            resetButton = new Button
            {
                Text = "Reset to 100%",
                Location = new Point(50, 160),
                Size = new Size(100, 30),
                Font = new Font("Segoe UI", 9F)
            };
            resetButton.Click += ResetButton_Click;
            this.Controls.Add(resetButton);

            // Minimize button
            minimizeButton = new Button
            {
                Text = "Minimize to Tray",
                Location = new Point(160, 160),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 9F)
            };
            minimizeButton.Click += MinimizeButton_Click;
            this.Controls.Add(minimizeButton);

            // Exit button
            exitButton = new Button
            {
                Text = "Exit",
                Location = new Point(290, 160),
                Size = new Size(60, 30),
                Font = new Font("Segoe UI", 9F)
            };
            exitButton.Click += ExitButton_Click;
            this.Controls.Add(exitButton);

            // Tray icon
            trayIcon = new NotifyIcon
            {
                Icon = new Icon("favicon.ico"),
                Text = "Screen Dimmer - Click to restore",
                Visible = false
            };
            trayIcon.Click += TrayIcon_Click;
        }

        private void InitializeApplication()
        {
            // Test gamma support and store original gamma ramp
            gammaSupported = TestGammaSupport();
            if (gammaSupported)
            {
                StoreOriginalGamma();
                currentDimmingMethod = DimmingMethod.GammaRamp;
            }
            else
            {
                currentDimmingMethod = DimmingMethod.Overlay;
            }

            UpdateStatusLabel();

            // Register global hotkey (Ctrl+Alt+F1)
            RegisterHotKey(this.Handle, 1, MOD_CONTROL | MOD_ALT, VK_F1);
        }

        private bool TestGammaSupport()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc == IntPtr.Zero) return false;

            RAMP testRamp = new RAMP
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };

            bool supported = GetDeviceGammaRamp(hdc, ref testRamp);
            ReleaseDC(IntPtr.Zero, hdc);

            return supported;
        }

        private void StoreOriginalGamma()
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                originalGammaRamp = new RAMP
                {
                    Red = new ushort[256],
                    Green = new ushort[256],
                    Blue = new ushort[256]
                };

                originalGammaStored = GetDeviceGammaRamp(hdc, ref originalGammaRamp);
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        private void RestoreOriginalGamma()
        {
            if (originalGammaStored)
            {
                IntPtr hdc = GetDC(IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    SetDeviceGammaRamp(hdc, ref originalGammaRamp);
                    ReleaseDC(IntPtr.Zero, hdc);
                }
            }
        }

        private void SetScreenBrightness(int brightness)
        {
            // Clamp brightness between 1 and 100
            brightness = Math.Max(1, Math.Min(100, brightness));

            switch (currentDimmingMethod)
            {
                case DimmingMethod.GammaRamp:
                    SetScreenBrightnessGamma(brightness);
                    break;
                case DimmingMethod.Overlay:
                    SetScreenBrightnessOverlay(brightness);
                    break;
                case DimmingMethod.Auto:
                default:
                    if (!TestGammaSupport())
                    {
                        currentDimmingMethod = DimmingMethod.Overlay;
                        UpdateStatusLabel("Method: Software Overlay (Auto Fallback)");
                        SetScreenBrightnessOverlay(brightness);
                    }
                    else
                    {
                        currentDimmingMethod = DimmingMethod.GammaRamp;
                        UpdateStatusLabel("Method: Hardware Gamma Ramp (Auto)");
                        SetScreenBrightnessGamma(brightness);
                    }
                    break;
            }
        }

        private void SetScreenBrightnessGamma(int brightness)
        {
            double gamma = (double)brightness / 100.0;

            RAMP gammaRamp = new RAMP
            {
                Red = new ushort[256],
                Green = new ushort[256],
                Blue = new ushort[256]
            };

            for (int i = 0; i < 256; i++)
            {
                ushort value = (ushort)(Math.Pow((double)i / 255.0, 1.0 / gamma) * 65535.0 * gamma);
                value = Math.Min(value, (ushort)65535);

                gammaRamp.Red[i] = value;
                gammaRamp.Green[i] = value;
                gammaRamp.Blue[i] = value;
            }

            IntPtr hdc = GetDC(IntPtr.Zero);
            if (hdc != IntPtr.Zero)
            {
                if (!SetDeviceGammaRamp(hdc, ref gammaRamp))
                {
                    if (userSelectedMethod == DimmingMethod.Auto)
                    {
                        gammaSupported = false;
                        currentDimmingMethod = DimmingMethod.Overlay;
                        UpdateStatusLabel("Method: Software Overlay (Auto Fallback)");
                        ReleaseDC(IntPtr.Zero, hdc);
                        SetScreenBrightnessOverlay(brightness);
                        return;
                    }
                    else
                    {
                        UpdateStatusLabel("Method: Hardware Gamma Ramp (FAILED)");
                        MessageBox.Show(
                            "Hardware Gamma Ramp failed!\n\n" +
                            "This may happen if:\n" +
                            "• Another application is controlling gamma\n" +
                            "• Display driver doesn't support gamma control\n" +
                            "• Running in remote desktop session\n\n" +
                            "Switch to 'Software Overlay' or 'Auto' mode for fallback support.",
                            "Gamma Control Failed",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    if (userSelectedMethod == DimmingMethod.GammaRamp)
                        UpdateStatusLabel("Method: Hardware Gamma Ramp");
                    else if (userSelectedMethod == DimmingMethod.Auto)
                        UpdateStatusLabel("Method: Hardware Gamma Ramp (Auto)");
                }
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        private void SetScreenBrightnessOverlay(int brightness)
        {
            if (brightness >= 100)
            {
                // Full brightness - hide overlay
                DestroyOverlayWindow();
                return;
            }

            // Create overlay window if it doesn't exist
            if (overlayForm == null)
            {
                CreateOverlayWindow();
            }

            if (overlayForm != null)
            {
                // Calculate opacity (0 = transparent, 255 = opaque)
                int opacity = (int)((100 - brightness) * 2.3);
                opacity = Math.Max(0, Math.Min(255, opacity));

                overlayForm.SetOpacity((byte)opacity);
                overlayForm.Show();
            }
        }

        private void CreateOverlayWindow()
        {
            overlayForm = new OverlayForm();
        }

        private void DestroyOverlayWindow()
        {
            if (overlayForm != null)
            {
                overlayForm.Close();
                overlayForm.Dispose();
                overlayForm = null;
            }
        }

        private void UpdateBrightnessLabel(int brightness)
        {
            brightnessLabel.Text = $"Brightness: {brightness}%";
        }

        private void UpdateStatusLabel(string? status = null)
        {
            if (status != null)
            {
                statusLabel.Text = status;
                return;
            }

            if (userSelectedMethod == DimmingMethod.Auto)
            {
                if (currentDimmingMethod == DimmingMethod.GammaRamp)
                    statusLabel.Text = "Method: Hardware Gamma Ramp (Auto)";
                else
                    statusLabel.Text = "Method: Software Overlay (Auto)";
            }
            else if (currentDimmingMethod == DimmingMethod.GammaRamp)
            {
                statusLabel.Text = "Method: Hardware Gamma Ramp";
            }
            else if (currentDimmingMethod == DimmingMethod.Overlay)
            {
                statusLabel.Text = "Method: Software Overlay";
            }
        }

        private void MinimizeToTray()
        {
            this.Hide();
            trayIcon.Visible = true;
            isMinimizedToTray = true;
        }

        private void RestoreFromTray()
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            trayIcon.Visible = false;
            isMinimizedToTray = false;
        }

        // Event handlers
        private void BrightnessSlider_ValueChanged(object? sender, EventArgs e)
        {
            currentBrightness = brightnessSlider.Value;
            SetScreenBrightness(currentBrightness);
            UpdateBrightnessLabel(currentBrightness);
        }

        private void ResetButton_Click(object? sender, EventArgs e)
        {
            brightnessSlider.Value = 100;
            currentBrightness = 100;
            SetScreenBrightness(100);
            UpdateBrightnessLabel(100);
        }

        private void MinimizeButton_Click(object? sender, EventArgs e)
        {
            MinimizeToTray();
        }

        private void ExitButton_Click(object? sender, EventArgs e)
        {
            this.Close();
        }

        private void TrayIcon_Click(object? sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void MethodComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // First, restore to full brightness and clean up current method
            if (currentDimmingMethod == DimmingMethod.GammaRamp && gammaSupported)
            {
                RestoreOriginalGamma();
            }
            else if (currentDimmingMethod == DimmingMethod.Overlay)
            {
                DestroyOverlayWindow();
            }

            // Set new method and track user selection
            switch (methodComboBox.SelectedIndex)
            {
                case 0: // Hardware Gamma Ramp
                    userSelectedMethod = DimmingMethod.GammaRamp;
                    currentDimmingMethod = DimmingMethod.GammaRamp;
                    if (TestGammaSupport())
                    {
                        StoreOriginalGamma();
                        UpdateStatusLabel("Method: Hardware Gamma Ramp");
                    }
                    else
                    {
                        UpdateStatusLabel("Method: Hardware Gamma Ramp (Not Supported)");
                        MessageBox.Show(
                            "Gamma ramp control is not supported on this system.\n" +
                            "Consider using 'Software Overlay' or 'Auto' mode instead.",
                            "Method Not Supported",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                    break;

                case 1: // Software Overlay
                    userSelectedMethod = DimmingMethod.Overlay;
                    currentDimmingMethod = DimmingMethod.Overlay;
                    UpdateStatusLabel("Method: Software Overlay");
                    break;

                case 2: // Auto
                default:
                    userSelectedMethod = DimmingMethod.Auto;
                    if (TestGammaSupport())
                    {
                        currentDimmingMethod = DimmingMethod.GammaRamp;
                        StoreOriginalGamma();
                        UpdateStatusLabel("Method: Hardware Gamma Ramp (Auto)");
                    }
                    else
                    {
                        currentDimmingMethod = DimmingMethod.Overlay;
                        UpdateStatusLabel("Method: Software Overlay (Auto)");
                    }
                    break;
            }

            // Reapply current brightness with new method
            SetScreenBrightness(currentBrightness);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY)
            {
                if (m.WParam.ToInt32() == 1) // Our registered hotkey
                {
                    if (isMinimizedToTray)
                        RestoreFromTray();
                    else
                        MinimizeToTray();
                }
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (gammaSupported)
            {
                RestoreOriginalGamma();
            }
            DestroyOverlayWindow();
            UnregisterHotKey(this.Handle, 1);
            if (isMinimizedToTray)
            {
                trayIcon.Visible = false;
            }
            trayIcon.Dispose();
            base.OnFormClosing(e);
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(value);
            if (value && this.WindowState == FormWindowState.Minimized)
            {
                MinimizeToTray();
            }
        }
    }
}
