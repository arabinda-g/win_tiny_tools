using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TinyTools
{
    internal static class Program
    {
        private static bool consoleMode = false;
        private static bool guiMode = true;

        [STAThread]
        static void Main(string[] args)
        {
            // Parse command line arguments
            if (args.Contains("--console") || args.Contains("-c"))
            {
                consoleMode = true;
                guiMode = false;
            }

            if (args.Contains("--help") || args.Contains("-h"))
            {
                ShowHelp();
                return;
            }

            if (args.Contains("--notepad3-only"))
            {
                consoleMode = true;
                guiMode = false;
                RunNotepad3Only();
                return;
            }

            // Initialize tools
            ToolManager.Instance.InitializeTools();
            SettingsManager.Instance.LoadSettings();

            if (consoleMode)
            {
                // Auto-start enabled modules in console mode
                ToolManager.Instance.AutoStartEnabledModules();
                ConsoleInterface.RunConsoleMode();
                return;
            }

            // GUI Mode
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool forceShow = args.Contains("--show");
            bool debugMode = args.Contains("--debug");

            if (debugMode)
            {
                AllocConsole();
                Console.WriteLine("Debug Mode: GUI + Console");
                Console.WriteLine("GUI window should appear...");
            }

            var mainForm = new MainForm(forceShow, debugMode);
            Application.Run(mainForm);
        }

        private static void ShowHelp()
        {
            AllocConsole();
            Console.WriteLine("TinyTools");
            Console.WriteLine("=========");
            Console.WriteLine("Usage:");
            Console.WriteLine("  (no args)         - Start GUI mode (default)");
            Console.WriteLine("  --console, -c     - Start in console mode");
            Console.WriteLine("  --notepad3-only   - Run only Notepad3 hotkey tool");
            Console.WriteLine("  --help, -h        - Show this help");
            Console.WriteLine();
            Console.WriteLine("GUI Mode Features:");
            Console.WriteLine("  - Visual tool management");
            Console.WriteLine("  - System tray integration");
            Console.WriteLine("  - Settings persistence");
            Console.WriteLine();
            Console.WriteLine("Console Mode Features:");
            Console.WriteLine("  - Command-line tool control");
            Console.WriteLine("  - Lightweight operation");
            Console.WriteLine("  - Scriptable interface");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            FreeConsole();
        }

        private static void RunNotepad3Only()
        {
            AllocConsole();
            Console.WriteLine("Notepad3 Hotkey Tool (Console Mode)");
            Console.WriteLine("Press Ctrl+W while Notepad3 is active to close it");
            Console.WriteLine("Press Ctrl+C to exit");
            Console.WriteLine("-------------------------------------------");

            ToolManager.Instance.InitializeTools();
            var tools = ToolManager.Instance.GetTools();
            var notepad3Tool = tools.FirstOrDefault(t => t.Name == "Notepad3 Hotkey");
            if (notepad3Tool != null)
            {
                notepad3Tool.Start();
            }

            // Simple message loop
            Application.Run();

            if (notepad3Tool != null)
            {
                notepad3Tool.Stop();
            }
            FreeConsole();
        }

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();
    }
}
