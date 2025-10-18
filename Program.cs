using System;
using System.Linq;
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
            // Initialize settings first to get log level
            SettingsManager.Instance.LoadSettings();
            
            // Initialize logging based on settings
            var logLevelString = SettingsManager.Instance.GetStringSetting("log_level");
            if (string.IsNullOrEmpty(logLevelString)) logLevelString = "Off";
            if (Enum.TryParse<LogLevel>(logLevelString, out var logLevel))
            {
                Logger.Instance.LogLevel = logLevel;
            }
            
            Logger.Instance.LogInfo($"TinyTools application starting with arguments: [{string.Join(", ", args)}]");
            
            // Parse command line arguments
            if (args.Contains("--console") || args.Contains("-c"))
            {
                Logger.Instance.LogInfo("Console mode requested via command line");
                consoleMode = true;
                guiMode = false;
            }

            if (args.Contains("--help") || args.Contains("-h"))
            {
                Logger.Instance.LogInfo("Help requested via command line");
                ShowHelp();
                return;
            }

            if (args.Contains("--notepad3-only"))
            {
                Logger.Instance.LogInfo("Notepad3-only mode requested via command line");
                consoleMode = true;
                guiMode = false;
                RunNotepad3Only();
                return;
            }

            // Initialize tools
            ToolManager.Instance.InitializeTools();

            if (consoleMode)
            {
                Logger.Instance.LogInfo("Starting in console mode");
                // Auto-start enabled modules in console mode
                ToolManager.Instance.AutoStartEnabledModules();
                ConsoleInterface.RunConsoleMode();
                Logger.Instance.LogInfo("Console mode ended");
                return;
            }

            // GUI Mode
            Logger.Instance.LogInfo("Starting in GUI mode");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool forceShow = args.Contains("--show");
            bool debugMode = args.Contains("--debug");

            Logger.Instance.LogDebug($"GUI mode options - ForceShow: {forceShow}, DebugMode: {debugMode}");

            if (debugMode)
            {
                Logger.Instance.LogInfo("Debug mode enabled - allocating console");
                AllocConsole();
                Console.WriteLine("Debug Mode: GUI + Console");
                Console.WriteLine("GUI window should appear...");
            }

            var mainForm = new MainForm(forceShow, debugMode);
            Logger.Instance.LogDebug("MainForm created, starting application message loop");
            Application.Run(mainForm);
            Logger.Instance.LogInfo("Application message loop ended");
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
            var notepad3Tool = ToolManager.Instance.GetTools().FirstOrDefault(t => t.Name == "Notepad3 Hotkey");
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

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool FreeConsole();
    }
}
