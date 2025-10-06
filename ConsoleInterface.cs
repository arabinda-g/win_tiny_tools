using System;
using System.Linq;

namespace TinyTools
{
    public static class ConsoleInterface
    {
        public static void RunConsoleMode()
        {
            AllocConsole();

            Console.WriteLine("Type 'help' for available commands");
            ShowConsoleTools();

            string input;
            while (true)
            {
                Console.Write("tiny-tools> ");
                input = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(input)) continue;

                var parts = input.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var command = parts[0].ToLower();

                switch (command)
                {
                    case "exit":
                        return;

                    case "help":
                        ShowConsoleHelp();
                        break;

                    case "list":
                        ShowConsoleTools();
                        break;

                    case "enable":
                    case "disable":
                    case "toggle":
                        if (parts.Length > 1 && int.TryParse(parts[1], out int id))
                        {
                            HandleToolCommand(command, id);
                        }
                        else
                        {
                            Console.WriteLine($"Please specify a tool ID. Example: {command} 1");
                        }
                        break;

                    case "settings":
                        ShowSettings();
                        break;

                    default:
                        Console.WriteLine("Unknown command. Type 'help' for available commands.");
                        break;
                }
            }
        }

        private static void ShowConsoleHelp()
        {
            Console.WriteLine();
            Console.WriteLine("Available Commands:");
            Console.WriteLine("  list          - Show all tools and their status");
            Console.WriteLine("  enable <id>   - Enable tool by ID (e.g., enable 1)");
            Console.WriteLine("  disable <id>  - Disable tool by ID");
            Console.WriteLine("  toggle <id>   - Toggle tool by ID");
            Console.WriteLine("  settings      - Show current settings");
            Console.WriteLine("  help          - Show this help");
            Console.WriteLine("  exit          - Exit the application");
            Console.WriteLine();
        }

        private static void ShowConsoleTools()
        {
            Console.WriteLine();
            Console.WriteLine("Available Tools:");
            Console.WriteLine("ID | Status   | Tool Name        | Description");
            Console.WriteLine("---|----------|------------------|-------------------------");

            var tools = ToolManager.Instance.GetTools();
            for (int i = 0; i < tools.Count; i++)
            {
                var tool = tools[i];
                var status = tool.Enabled ? "Enabled " : "Disabled";
                Console.WriteLine($"{i + 1,2} | {status,-8} | {tool.Name,-16} | {tool.Description}");
            }
            Console.WriteLine();
        }

        private static void HandleToolCommand(string command, int id)
        {
            var tools = ToolManager.Instance.GetTools();
            if (id < 1 || id > tools.Count)
            {
                Console.WriteLine("Invalid tool ID. Use 'list' to see available tools.");
                return;
            }

            var tool = tools[id - 1];
            
            switch (command)
            {
                case "enable":
                    if (!tool.Enabled)
                    {
                        ToolManager.Instance.ToggleTool(id - 1);
                        Console.WriteLine($"✓ Enabled: {tool.Name}");
                    }
                    else
                    {
                        Console.WriteLine("Tool is already enabled.");
                    }
                    break;

                case "disable":
                    if (tool.Enabled)
                    {
                        ToolManager.Instance.ToggleTool(id - 1);
                        Console.WriteLine($"✗ Disabled: {tool.Name}");
                    }
                    else
                    {
                        Console.WriteLine("Tool is already disabled.");
                    }
                    break;

                case "toggle":
                    ToolManager.Instance.ToggleTool(id - 1);
                    var newStatus = tool.Enabled ? "Enabled" : "Disabled";
                    var symbol = tool.Enabled ? "✓" : "✗";
                    Console.WriteLine($"{symbol} {newStatus}: {tool.Name}");
                    break;
            }
        }

        private static void ShowSettings()
        {
            Console.WriteLine();
            Console.WriteLine("Current Settings:");
            var settings = SettingsManager.Instance.GetAllSettings();
            foreach (var pair in settings)
            {
                Console.WriteLine($"  {pair.Key} = {pair.Value}");
            }
            Console.WriteLine();
        }

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        private static extern bool AllocConsole();
    }
}
