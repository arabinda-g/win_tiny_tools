using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Reflection;

namespace TinyTools.Modules.ScreenDimmer
{
    public class ScreenDimmerManager
    {
        private static readonly Lazy<ScreenDimmerManager> instance = new(() => new ScreenDimmerManager());
        public static ScreenDimmerManager Instance => instance.Value;

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
        private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

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

        // Events
        public event EventHandler<int>? BrightnessChanged;
        public event EventHandler<DimmingMethod>? MethodChanged;

        // Private fields
        private ScreenDimmerSettingsForm? settingsForm;
        private ScreenDimmerOverlayForm? overlayForm;
        private NotifyIcon? trayIcon;
        
        private int currentBrightness = 100;
        private RAMP originalGammaRamp;
        private bool originalGammaStored = false;
        private DimmingMethod currentDimmingMethod = DimmingMethod.Auto;
        private DimmingMethod userSelectedMethod = DimmingMethod.Auto;
        private bool gammaSupported = true;
        private bool isRunning = false;

        // Configuration properties
        public int Brightness
        {
            get => currentBrightness;
            set
            {
                if (value != currentBrightness)
                {
                    currentBrightness = Math.Max(10, Math.Min(100, value));
                    if (isRunning)
                    {
                        SetScreenBrightness(currentBrightness);
                    }
                    BrightnessChanged?.Invoke(this, currentBrightness);
                }
            }
        }

        public DimmingMethod Method
        {
            get => userSelectedMethod;
            set
            {
                if (value != userSelectedMethod)
                {
                    userSelectedMethod = value;
                    if (isRunning)
                    {
                        ApplyDimmingMethod();
                    }
                    MethodChanged?.Invoke(this, userSelectedMethod);
                }
            }
        }

        private ScreenDimmerManager() { }

        private static Icon GetEmbeddedScreenDimmerIcon()
        {
            // Use the embedded ScreenDimmer-specific icon
            return ScreenDimmerResources.GetScreenDimmerIcon();
        }

        public void StartDimmer()
        {
            if (isRunning) return;

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

            // Create system tray icon
            CreateTrayIcon();

            // Apply current settings
            ApplyDimmingMethod();
            SetScreenBrightness(currentBrightness);

            isRunning = true;
            Console.WriteLine("✓ ScreenDimmer started");
        }

        public void StopDimmer()
        {
            if (!isRunning) return;

            // Restore original gamma
            if (gammaSupported && originalGammaStored)
            {
                RestoreOriginalGamma();
            }

            // Destroy overlay
            DestroyOverlayWindow();

            // Hide tray icon
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }

            // Close settings window
            if (settingsForm != null && !settingsForm.IsDisposed)
            {
                settingsForm.Close();
            }

            isRunning = false;
            Console.WriteLine("✗ ScreenDimmer stopped");
        }

        public void ShowSettingsWindow()
        {
            if (settingsForm == null || settingsForm.IsDisposed)
            {
                settingsForm = new ScreenDimmerSettingsForm(this);
            }

            if (settingsForm.Visible)
            {
                settingsForm.BringToFront();
            }
            else
            {
                settingsForm.Show();
            }
        }

        private void CreateTrayIcon()
        {
            if (trayIcon != null) return;

            try
            {
                // Use embedded icon from executable resources
                Icon screenDimmerIcon = GetEmbeddedScreenDimmerIcon();

                trayIcon = new NotifyIcon
                {
                    Icon = screenDimmerIcon,
                    Text = "Screen Dimmer - Right-click for options",
                    Visible = true
                };

                // Create context menu
                var contextMenu = new ContextMenuStrip();
                
                var settingsItem = new ToolStripMenuItem("Settings...");
                settingsItem.Click += (s, e) => ShowSettingsWindow();
                contextMenu.Items.Add(settingsItem);

                contextMenu.Items.Add(new ToolStripSeparator());

                var brightnessMenu = new ToolStripMenuItem("Brightness");
                for (int i = 100; i >= 10; i -= 10)
                {
                    var brightnessItem = new ToolStripMenuItem($"{i}%");
                    int brightness = i; // Capture for closure
                    brightnessItem.Click += (s, e) => Brightness = brightness;
                    brightnessMenu.DropDownItems.Add(brightnessItem);
                }
                contextMenu.Items.Add(brightnessMenu);

                contextMenu.Items.Add(new ToolStripSeparator());

                var resetItem = new ToolStripMenuItem("Reset to 100%");
                resetItem.Click += (s, e) => Brightness = 100;
                contextMenu.Items.Add(resetItem);

                trayIcon.ContextMenuStrip = contextMenu;
                trayIcon.DoubleClick += (s, e) => ShowSettingsWindow();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create ScreenDimmer tray icon: {ex.Message}");
            }
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

        private void ApplyDimmingMethod()
        {
            // Clean up current method
            if (currentDimmingMethod == DimmingMethod.GammaRamp && gammaSupported)
            {
                RestoreOriginalGamma();
            }
            else if (currentDimmingMethod == DimmingMethod.Overlay)
            {
                DestroyOverlayWindow();
            }

            // Set new method
            switch (userSelectedMethod)
            {
                case DimmingMethod.GammaRamp:
                    currentDimmingMethod = DimmingMethod.GammaRamp;
                    if (TestGammaSupport())
                    {
                        StoreOriginalGamma();
                    }
                    break;

                case DimmingMethod.Overlay:
                    currentDimmingMethod = DimmingMethod.Overlay;
                    break;

                case DimmingMethod.Auto:
                default:
                    if (TestGammaSupport())
                    {
                        currentDimmingMethod = DimmingMethod.GammaRamp;
                        StoreOriginalGamma();
                    }
                    else
                    {
                        currentDimmingMethod = DimmingMethod.Overlay;
                    }
                    break;
            }
        }

        private void SetScreenBrightness(int brightness)
        {
            brightness = Math.Max(10, Math.Min(100, brightness));

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
                        SetScreenBrightnessOverlay(brightness);
                    }
                    else
                    {
                        currentDimmingMethod = DimmingMethod.GammaRamp;
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
                        ReleaseDC(IntPtr.Zero, hdc);
                        SetScreenBrightnessOverlay(brightness);
                        return;
                    }
                }
                ReleaseDC(IntPtr.Zero, hdc);
            }
        }

        private void SetScreenBrightnessOverlay(int brightness)
        {
            if (brightness >= 100)
            {
                DestroyOverlayWindow();
                return;
            }

            if (overlayForm == null)
            {
                CreateOverlayWindow();
            }

            if (overlayForm != null)
            {
                int opacity = (int)((100 - brightness) * 2.3);
                opacity = Math.Max(0, Math.Min(255, opacity));

                overlayForm.SetOpacity((byte)opacity);
                overlayForm.Show();
            }
        }

        private void CreateOverlayWindow()
        {
            overlayForm = new ScreenDimmerOverlayForm();
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
    }
}
