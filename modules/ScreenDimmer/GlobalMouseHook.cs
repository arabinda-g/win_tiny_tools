using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace TinyTools.Modules.ScreenDimmer
{
    public class GlobalMouseHook
    {
        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;
        
        private LowLevelMouseProc _proc = HookCallback;
        private IntPtr _hookID = IntPtr.Zero;
        private static GlobalMouseHook? _instance;
        
        public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        
        // Event for mouse wheel with modifiers
        public static event EventHandler<MouseWheelEventArgs>? GlobalMouseWheel;
        
        public class MouseWheelEventArgs : EventArgs
        {
            public int Delta { get; set; }
            public bool CtrlPressed { get; set; }
            public bool ShiftPressed { get; set; }
            public bool AltPressed { get; set; }
        }
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode,
            IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        
        [DllImport("user32.dll")]
        private static extern short GetKeyState(int nVirtKey);
        
        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }
        
        private GlobalMouseHook()
        {
            _hookID = SetHook(_proc);
        }
        
        public static GlobalMouseHook Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new GlobalMouseHook();
                }
                return _instance;
            }
        }
        
        public void Start()
        {
            if (_hookID == IntPtr.Zero)
            {
                _hookID = SetHook(_proc);
            }
        }
        
        public void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }
        
        private IntPtr SetHook(LowLevelMouseProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule? curModule = curProcess.MainModule)
            {
                if (curModule?.ModuleName != null)
                {
                    return SetWindowsHookEx(WH_MOUSE_LL, proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }
            }
            return IntPtr.Zero;
        }

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEWHEEL)
            {
                try
                {
                    MSLLHOOKSTRUCT hookStruct = (MSLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(MSLLHOOKSTRUCT))!;
                    
                    // Extract wheel delta from mouseData (high word)
                    short delta = (short)((hookStruct.mouseData >> 16) & 0xffff);
                    
                    // Check modifier keys
                    bool ctrlPressed = (GetKeyState(0x11) & 0x8000) != 0; // VK_CONTROL
                    bool shiftPressed = (GetKeyState(0x10) & 0x8000) != 0; // VK_SHIFT
                    bool altPressed = (GetKeyState(0x12) & 0x8000) != 0; // VK_MENU
                    
                    // Fire the event
                    GlobalMouseWheel?.Invoke(null, new MouseWheelEventArgs
                    {
                        Delta = delta,
                        CtrlPressed = ctrlPressed,
                        ShiftPressed = shiftPressed,
                        AltPressed = altPressed
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in mouse hook callback: {ex.Message}");
                }
            }

            return CallNextHookEx(_instance?._hookID ?? IntPtr.Zero, nCode, wParam, lParam);
        }
        
        ~GlobalMouseHook()
        {
            Stop();
        }
    }
}
