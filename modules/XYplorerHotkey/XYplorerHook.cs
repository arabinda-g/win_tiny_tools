using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TinyTools
{
    public class XYplorerHook
    {
        private static readonly Lazy<XYplorerHook> instance = new(() => new XYplorerHook());
        public static XYplorerHook Instance => instance.Value;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_E = 0x45;

        private const string XYplorerPath = @"C:\Program Files\XYplorer\XYplorer.exe";

        private LowLevelKeyboardProc proc;
        private IntPtr hookId = IntPtr.Zero;
        private bool winPressed = false;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private XYplorerHook()
        {
            proc = HookCallback;
        }

        public void StartHook()
        {
            if (hookId == IntPtr.Zero)
            {
                hookId = SetHook(proc);
                if (hookId == IntPtr.Zero)
                {
                    var error = Marshal.GetLastWin32Error();
                    MessageBox.Show($"Failed to install keyboard hook. Error code: {error}", 
                                  "Hook Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    Console.WriteLine("✓ XYplorer hotkey monitoring started (Win+E)");
                }
            }
        }

        public void StopHook()
        {
            if (hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
                Console.WriteLine("✗ XYplorer hotkey monitoring stopped");
            }
        }

        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (var curProcess = Process.GetCurrentProcess())
            {
                var curModule = curProcess.MainModule;
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                var vkCode = Marshal.ReadInt32(lParam);

                // Track Win key state
                if (vkCode == VK_LWIN || vkCode == VK_RWIN)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                    {
                        winPressed = true;
                    }
                    else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                    {
                        winPressed = false;
                    }
                }

                // Check for Win+E
                if (vkCode == VK_E && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
                {
                    if (winPressed)
                    {
                        // Launch XYplorer
                        try
                        {
                            if (System.IO.File.Exists(XYplorerPath))
                            {
                                Process.Start(new ProcessStartInfo
                                {
                                    FileName = XYplorerPath,
                                    UseShellExecute = true
                                });
                                Console.WriteLine("XYplorer launched via Win+E");
                            }
                            else
                            {
                                Console.WriteLine($"XYplorer not found at: {XYplorerPath}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to launch XYplorer: {ex.Message}");
                        }
                        
                        return (IntPtr)1; // Block the key to prevent Windows Explorer from opening
                    }
                }
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
