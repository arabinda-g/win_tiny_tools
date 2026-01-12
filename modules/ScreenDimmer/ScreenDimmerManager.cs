using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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

        [DllImport("gdi32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr CreateDC(string lpszDriver, string lpszDevice, string? lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

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

        // Monitor information class
        public class MonitorInfo
        {
            public IntPtr Handle { get; set; }
            public Rectangle Bounds { get; set; }
            public Rectangle WorkArea { get; set; }
            public string DeviceName { get; set; } = "";
            public bool IsPrimary { get; set; }
            public bool IsSelected { get; set; } = true;
            public RAMP OriginalGammaRamp { get; set; }
            public bool OriginalGammaStored { get; set; }
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
        private Dictionary<IntPtr, ScreenDimmerOverlayForm> overlayForms = new Dictionary<IntPtr, ScreenDimmerOverlayForm>();
        private NotifyIcon? trayIcon;
        private GlobalMouseHook? globalMouseHook;
        
        private int currentBrightness = 100;
        private List<MonitorInfo> monitors = new List<MonitorInfo>();
        private List<MonitorInfo> selectedMonitors = new List<MonitorInfo>();
        private RAMP originalGammaRamp;
        private bool originalGammaStored = false;
        private DimmingMethod currentDimmingMethod = DimmingMethod.Auto;
        private DimmingMethod userSelectedMethod = DimmingMethod.Auto;
        private bool gammaSupported = true;
        private bool isRunning = false;
        private bool globalHotkeyEnabled = true;

        // Configuration properties
        public int Brightness
        {
            get => currentBrightness;
            set
            {
                if (value != currentBrightness)
                {
                    currentBrightness = Math.Max(1, Math.Min(100, value));
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

        public bool GlobalHotkeyEnabled
        {
            get => globalHotkeyEnabled;
            set
            {
                if (value != globalHotkeyEnabled)
                {
                    globalHotkeyEnabled = value;
                    if (isRunning)
                    {
                        if (globalHotkeyEnabled)
                        {
                            StartGlobalHotkey();
                        }
                        else
                        {
                            StopGlobalHotkey();
                        }
                    }
                    // Save to config
                    ScreenDimmerConfig.Instance.GlobalHotkeyEnabled = value;
                }
            }
        }

        private ScreenDimmerManager() 
        {
            // Load settings from config
            LoadSettings();
            // Detect monitors
            DetectMonitors();
        }

        private void LoadSettings()
        {
            var config = ScreenDimmerConfig.Instance;
            currentBrightness = config.Brightness;
            userSelectedMethod = config.DimmingMethod;
            globalHotkeyEnabled = config.GlobalHotkeyEnabled;
        }

        private void DetectMonitors()
        {
            monitors.Clear();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, MonitorEnumProc, IntPtr.Zero);
            selectedMonitors = monitors.Where(m => m.IsSelected).ToList();
        }

        private bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
        {
            MONITORINFOEX mi = new MONITORINFOEX();
            mi.cbSize = Marshal.SizeOf(mi);
            
            if (GetMonitorInfo(hMonitor, ref mi))
            {
                var monitor = new MonitorInfo
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

        public void UpdateSelectedMonitors(List<MonitorInfo> newSelectedMonitors)
        {
            selectedMonitors = newSelectedMonitors;
            
            // Apply current brightness to newly selected monitors
            if (isRunning)
            {
                SetScreenBrightness(currentBrightness);
            }
        }

        public void ResetMonitorBrightness(MonitorInfo monitor)
        {
            if (currentDimmingMethod == DimmingMethod.GammaRamp && monitor.OriginalGammaStored)
            {
                // Restore original gamma for this specific monitor using its device name
                IntPtr hdc = CreateDC(monitor.DeviceName, monitor.DeviceName, null, IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    var gammaRamp = monitor.OriginalGammaRamp;
                    SetDeviceGammaRamp(hdc, ref gammaRamp);
                    DeleteDC(hdc);
                }
            }
            else if (currentDimmingMethod == DimmingMethod.Overlay)
            {
                // Remove overlay for this monitor
                if (overlayForms.ContainsKey(monitor.Handle))
                {
                    overlayForms[monitor.Handle].Close();
                    overlayForms[monitor.Handle].Dispose();
                    overlayForms.Remove(monitor.Handle);
                }
            }
        }

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

            // Start global hotkey if enabled
            if (globalHotkeyEnabled)
            {
                StartGlobalHotkey();
            }

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

            // Destroy overlays
            DestroyOverlayWindows();

            // Stop global hotkey
            StopGlobalHotkey();

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
                // If window is minimized, restore it
                if (settingsForm.WindowState == FormWindowState.Minimized)
                {
                    settingsForm.WindowState = FormWindowState.Normal;
                }
                
                // Bring to front and activate
                settingsForm.BringToFront();
                settingsForm.Activate();
            }
            else
            {
                settingsForm.Show();
                settingsForm.Activate();
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
                trayIcon.Click += (s, e) => ShowSettingsWindow();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create ScreenDimmer tray icon: {ex.Message}");
            }
        }

        private bool TestGammaSupport()
        {
            // Test gamma support on each selected monitor
            foreach (var monitor in selectedMonitors)
            {
                IntPtr hdc = CreateDC(monitor.DeviceName, monitor.DeviceName, null, IntPtr.Zero);
                if (hdc == IntPtr.Zero) continue;

                RAMP testRamp = new RAMP
                {
                    Red = new ushort[256],
                    Green = new ushort[256],
                    Blue = new ushort[256]
                };

                bool supported = GetDeviceGammaRamp(hdc, ref testRamp);
                DeleteDC(hdc);

                if (supported) return true;
            }

            // Fallback to primary DC if no monitors selected
            if (selectedMonitors.Count == 0)
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

            return false;
        }

        private void StoreOriginalGamma()
        {
            // Store original gamma for each monitor
            foreach (var monitor in monitors)
            {
                IntPtr hdc = CreateDC(monitor.DeviceName, monitor.DeviceName, null, IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    var ramp = new RAMP
                    {
                        Red = new ushort[256],
                        Green = new ushort[256],
                        Blue = new ushort[256]
                    };

                    if (GetDeviceGammaRamp(hdc, ref ramp))
                    {
                        monitor.OriginalGammaRamp = ramp;
                        monitor.OriginalGammaStored = true;
                    }
                    DeleteDC(hdc);
                }
            }
            originalGammaStored = monitors.Any(m => m.OriginalGammaStored);
        }

        private void RestoreOriginalGamma()
        {
            // Restore original gamma for each monitor
            foreach (var monitor in monitors)
            {
                if (monitor.OriginalGammaStored)
                {
                    IntPtr hdc = CreateDC(monitor.DeviceName, monitor.DeviceName, null, IntPtr.Zero);
                    if (hdc != IntPtr.Zero)
                    {
                        var ramp = monitor.OriginalGammaRamp;
                        SetDeviceGammaRamp(hdc, ref ramp);
                        DeleteDC(hdc);
                    }
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
                DestroyOverlayWindows();
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

            bool anySuccess = false;

            // Apply gamma to each selected monitor
            foreach (var monitor in selectedMonitors)
            {
                IntPtr hdc = CreateDC(monitor.DeviceName, monitor.DeviceName, null, IntPtr.Zero);
                if (hdc != IntPtr.Zero)
                {
                    if (SetDeviceGammaRamp(hdc, ref gammaRamp))
                    {
                        anySuccess = true;
                    }
                    DeleteDC(hdc);
                }
            }

            // Fallback to overlay if gamma failed on all monitors
            if (!anySuccess && selectedMonitors.Count > 0 && userSelectedMethod == DimmingMethod.Auto)
            {
                gammaSupported = false;
                currentDimmingMethod = DimmingMethod.Overlay;
                SetScreenBrightnessOverlay(brightness);
            }
        }

        private void SetScreenBrightnessOverlay(int brightness)
        {
            if (brightness >= 100)
            {
                DestroyOverlayWindows();
                return;
            }

            // Create overlay for each selected monitor
            foreach (var monitor in selectedMonitors)
            {
                if (!overlayForms.ContainsKey(monitor.Handle))
                {
                    CreateOverlayWindow(monitor);
                }

                if (overlayForms.ContainsKey(monitor.Handle))
                {
                    int opacity = (int)((100 - brightness) * 2.3);
                    opacity = Math.Max(0, Math.Min(255, opacity));

                    overlayForms[monitor.Handle].SetOpacity((byte)opacity);
                    overlayForms[monitor.Handle].Show();
                }
            }

            // Remove overlays for unselected monitors
            var monitorsToRemove = overlayForms.Keys.Where(handle => 
                !selectedMonitors.Any(m => m.Handle == handle)).ToList();
            
            foreach (var handle in monitorsToRemove)
            {
                overlayForms[handle].Close();
                overlayForms[handle].Dispose();
                overlayForms.Remove(handle);
            }
        }

        private void CreateOverlayWindow(MonitorInfo monitor)
        {
            var overlayForm = new ScreenDimmerOverlayForm();
            overlayForm.SetBounds(monitor.Bounds.X, monitor.Bounds.Y, monitor.Bounds.Width, monitor.Bounds.Height);
            overlayForms[monitor.Handle] = overlayForm;
        }

        private void DestroyOverlayWindows()
        {
            foreach (var overlay in overlayForms.Values)
            {
                overlay.Close();
                overlay.Dispose();
            }
            overlayForms.Clear();
        }

        private void StartGlobalHotkey()
        {
            try
            {
                if (globalMouseHook == null)
                {
                    globalMouseHook = GlobalMouseHook.Instance;
                    GlobalMouseHook.GlobalMouseWheel += OnGlobalMouseWheel;
                }
                globalMouseHook.Start();
                Console.WriteLine("✓ Global hotkey (Ctrl+Shift+Mouse Wheel) enabled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to start global hotkey: {ex.Message}");
            }
        }

        private void StopGlobalHotkey()
        {
            try
            {
                if (globalMouseHook != null)
                {
                    GlobalMouseHook.GlobalMouseWheel -= OnGlobalMouseWheel;
                    globalMouseHook.Stop();
                    globalMouseHook = null;
                }
                Console.WriteLine("✗ Global hotkey disabled");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping global hotkey: {ex.Message}");
            }
        }

        private void OnGlobalMouseWheel(object? sender, GlobalMouseHook.MouseWheelEventArgs e)
        {
            // Check if Ctrl+Shift is pressed (and Alt is not pressed to avoid conflicts)
            if (e.CtrlPressed && e.ShiftPressed && !e.AltPressed)
            {
                // Calculate brightness change (1% per wheel notch)
                int brightnessChange = (e.Delta > 0) ? 1 : -1;
                int newBrightness = Math.Max(1, Math.Min(100, currentBrightness + brightnessChange));
                
                if (newBrightness != currentBrightness)
                {
                    Brightness = newBrightness;
                    Console.WriteLine($"Global hotkey: Brightness changed to {newBrightness}%");
                }
            }
        }
    }
}
