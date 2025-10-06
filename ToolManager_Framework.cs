using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyTools.Modules.ScreenDimmer;

namespace TinyTools
{
    public class ToolManager
    {
        private static ToolManager instance;
        public static ToolManager Instance
        {
            get
            {
                if (instance == null)
                    instance = new ToolManager();
                return instance;
            }
        }

        private readonly List<ToolModule> tools = new List<ToolModule>();
        private readonly object toolsLock = new object();

        private ToolManager() { }

        public void InitializeTools()
        {
            lock (toolsLock)
            {
                tools.Clear();

                // Add Notepad3 hotkey tool
                var notepad3Tool = new ToolModule("Notepad3 Hotkey", "Close Notepad3 with Ctrl+W");
                notepad3Tool.SetStartFunction(delegate() { Notepad3Hook.Instance.StartHook(); });
                notepad3Tool.SetStopFunction(delegate() { Notepad3Hook.Instance.StopHook(); });
                notepad3Tool.Enabled = true; // Default enabled
                tools.Add(notepad3Tool);

                // Add ScreenDimmer tool
                var screenDimmerTool = new ToolModule("Screen Dimmer", "Control screen brightness with gamma ramp or overlay");
                screenDimmerTool.SetStartFunction(delegate() {
                    var config = ScreenDimmerConfig.Instance;
                    var manager = ScreenDimmerManager.Instance;
                    
                    // Load saved settings
                    manager.Brightness = config.Brightness;
                    manager.Method = config.DimmingMethod;
                    
                    // Start the dimmer
                    manager.StartDimmer();
                    
                    // Subscribe to changes to save them
                    manager.BrightnessChanged += delegate(object s, int brightness) {
                        config.Brightness = brightness;
                    };
                    manager.MethodChanged += delegate(object s, ScreenDimmerManager.DimmingMethod method) {
                        config.DimmingMethod = method;
                    };
                });
                screenDimmerTool.SetStopFunction(delegate() {
                    ScreenDimmerManager.Instance.StopDimmer();
                });
                screenDimmerTool.SetSettingsFunction(delegate() {
                    ScreenDimmerManager.Instance.ShowSettingsWindow();
                });
                screenDimmerTool.Enabled = ScreenDimmerConfig.Instance.Enabled;
                tools.Add(screenDimmerTool);

                // Load saved module states
                LoadModuleStates();
            }
        }

        public List<ToolModule> GetTools()
        {
            lock (toolsLock)
            {
                return new List<ToolModule>(tools);
            }
        }

        public ToolModule GetTool(int index)
        {
            lock (toolsLock)
            {
                if (index >= 0 && index < tools.Count)
                    return tools[index];
                return null;
            }
        }

        public void ToggleTool(int index)
        {
            lock (toolsLock)
            {
                if (index >= 0 && index < tools.Count)
                {
                    var tool = tools[index];
                    tool.Toggle();
                    
                    // Save ScreenDimmer enabled state to its config
                    if (tool.Name == "Screen Dimmer")
                    {
                        ScreenDimmerConfig.Instance.Enabled = tool.Enabled;
                    }
                    
                    SaveModuleStates();
                }
            }
        }

        public void AutoStartEnabledModules()
        {
            lock (toolsLock)
            {
                foreach (var tool in tools)
                {
                    if (tool.Enabled && !tool.IsRunning)
                    {
                        tool.Start();
                    }
                }
            }
        }

        public void StopAllTools()
        {
            lock (toolsLock)
            {
                foreach (var tool in tools)
                {
                    if (tool.IsRunning)
                    {
                        tool.Stop();
                    }
                }
            }
        }

        private void SaveModuleStates()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tool_manager_modules.ini");
            var lines = new List<string>();
            foreach (var tool in tools)
            {
                lines.Add(tool.Name + "=" + (tool.Enabled ? "1" : "0"));
            }
            File.WriteAllLines(configPath, lines.ToArray());
        }

        private void LoadModuleStates()
        {
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tool_manager_modules.ini");
            if (!File.Exists(configPath)) return;

            var moduleStates = new Dictionary<string, bool>();
            foreach (var line in File.ReadAllLines(configPath))
            {
                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    moduleStates[parts[0]] = parts[1] == "1" || parts[1].ToLower() == "true";
                }
            }

            // Apply states to tools (default to enabled if not found)
            foreach (var tool in tools)
            {
                if (moduleStates.ContainsKey(tool.Name))
                {
                    tool.Enabled = moduleStates[tool.Name];
                }
                else
                {
                    tool.Enabled = true; // Default to enabled
                }
            }
        }
    }
}
