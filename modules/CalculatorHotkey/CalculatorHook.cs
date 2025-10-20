using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TinyTools
{
    public class CalculatorHook
    {
        private static readonly Lazy<CalculatorHook> instance = new(() => new CalculatorHook());
        public static CalculatorHook Instance => instance.Value;

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYUP = 0x0105;
        private const int VK_LCONTROL = 0xA2;
        private const int VK_RCONTROL = 0xA3;
        private const int VK_W = 0x57;

        private LowLevelKeyboardProc proc;
        private IntPtr hookId = IntPtr.Zero;
        private bool ctrlPressed = false;

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private CalculatorHook()
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
                    Console.WriteLine("✓ Calculator hotkey monitoring started (Ctrl+W)");
                }
            }
        }

        public void StopHook()
        {
            if (hookId != IntPtr.Zero)
            {
                UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
                Console.WriteLine("✗ Calculator hotkey monitoring stopped");
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

                if (vkCode == VK_LCONTROL || vkCode == VK_RCONTROL)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                    {
                        ctrlPressed = true;
                    }
                    else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
                    {
                        ctrlPressed = false;
                    }
                }

                if (vkCode == VK_W && (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN))
                {
                    if (ctrlPressed && IsCalculatorActive())
                    {
                        var activeWindow = GetForegroundWindow();
                        if (activeWindow != IntPtr.Zero)
                        {
                            PostMessage(activeWindow, 0x0010, IntPtr.Zero, IntPtr.Zero); // WM_CLOSE
                            Console.WriteLine("Calculator window closed via Ctrl+W");
                        }
                        return (IntPtr)1; // Block the key
                    }
                }
            }

            return CallNextHookEx(hookId, nCode, wParam, lParam);
        }

        private bool IsCalculatorActive()
        {
            var activeWindow = GetForegroundWindow();
            if (activeWindow == IntPtr.Zero) return false;

            GetWindowThreadProcessId(activeWindow, out uint processId);
            
            try
            {
                using (var process = Process.GetProcessById((int)processId))
                {
                    return process.ProcessName.ToLower() == "win32calc";
                }
            }
            catch
            {
                return false;
            }
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    }
}
