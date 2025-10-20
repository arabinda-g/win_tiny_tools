using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TinyTools.Modules.ScreenDimmer;

namespace TinyTools
{
    public class ToolManager
    {
        private static readonly Lazy<ToolManager> instance = new(() => new ToolManager());
        public static ToolManager Instance => instance.Value;

        private readonly List<ToolModule> tools = new();
        private readonly object toolsLock = new();

        private ToolManager() { }

        public void InitializeTools()
        {
            Logger.Instance.LogInfo("Initializing tools...");
            lock (toolsLock)
            {
                tools.Clear();
                Logger.Instance.LogDebug("Cleared existing tools list");

                // Add Notepad3 hotkey tool
                Logger.Instance.LogDebug("Adding Notepad3 Hotkey tool");
                var notepad3Tool = new ToolModule("Notepad3 Hotkey", "Close Notepad3 with Ctrl+W");
                notepad3Tool.SetStartFunction(() => {
                    Logger.Instance.LogInfo("Starting Notepad3 Hook");
                    Notepad3Hook.Instance.StartHook();
                    Logger.Instance.LogDebug("Notepad3 Hook started successfully");
                });
                notepad3Tool.SetStopFunction(() => {
                    Logger.Instance.LogInfo("Stopping Notepad3 Hook");
                    Notepad3Hook.Instance.StopHook();
                    Logger.Instance.LogDebug("Notepad3 Hook stopped successfully");
                });
                notepad3Tool.Enabled = true; // Default enabled
                tools.Add(notepad3Tool);
                Logger.Instance.LogTrace("Notepad3 Hotkey tool added to tools list");

                // Add Paint hotkey tool
                Logger.Instance.LogDebug("Adding Paint Hotkey tool");
                var paintTool = new ToolModule("Paint Hotkey", "Close Paint with Ctrl+W");
                paintTool.SetStartFunction(() => {
                    Logger.Instance.LogInfo("Starting Paint Hook");
                    PaintHook.Instance.StartHook();
                    Logger.Instance.LogDebug("Paint Hook started successfully");
                });
                paintTool.SetStopFunction(() => {
                    Logger.Instance.LogInfo("Stopping Paint Hook");
                    PaintHook.Instance.StopHook();
                    Logger.Instance.LogDebug("Paint Hook stopped successfully");
                });
                paintTool.Enabled = true; // Default enabled
                tools.Add(paintTool);
                Logger.Instance.LogTrace("Paint Hotkey tool added to tools list");

                // Add Calculator hotkey tool
                Logger.Instance.LogDebug("Adding Calculator Hotkey tool");
                var calculatorTool = new ToolModule("Calculator Hotkey", "Close Calculator with Ctrl+W");
                calculatorTool.SetStartFunction(() => {
                    Logger.Instance.LogInfo("Starting Calculator Hook");
                    CalculatorHook.Instance.StartHook();
                    Logger.Instance.LogDebug("Calculator Hook started successfully");
                });
                calculatorTool.SetStopFunction(() => {
                    Logger.Instance.LogInfo("Stopping Calculator Hook");
                    CalculatorHook.Instance.StopHook();
                    Logger.Instance.LogDebug("Calculator Hook stopped successfully");
                });
                calculatorTool.Enabled = true; // Default enabled
                tools.Add(calculatorTool);
                Logger.Instance.LogTrace("Calculator Hotkey tool added to tools list");

                // Add Snipping Tool hotkey tool
                Logger.Instance.LogDebug("Adding Snipping Tool Hotkey tool");
                var snippingToolTool = new ToolModule("Snipping Tool Hotkey", "Close Snipping Tool with Ctrl+W");
                snippingToolTool.SetStartFunction(() => {
                    Logger.Instance.LogInfo("Starting Snipping Tool Hook");
                    SnippingToolHook.Instance.StartHook();
                    Logger.Instance.LogDebug("Snipping Tool Hook started successfully");
                });
                snippingToolTool.SetStopFunction(() => {
                    Logger.Instance.LogInfo("Stopping Snipping Tool Hook");
                    SnippingToolHook.Instance.StopHook();
                    Logger.Instance.LogDebug("Snipping Tool Hook stopped successfully");
                });
                snippingToolTool.Enabled = true; // Default enabled
                tools.Add(snippingToolTool);
                Logger.Instance.LogTrace("Snipping Tool Hotkey tool added to tools list");

                // Add ScreenDimmer tool
                Logger.Instance.LogDebug("Adding Screen Dimmer tool");
                var screenDimmerTool = new ToolModule("Screen Dimmer", "Control screen brightness with gamma ramp or overlay");
                screenDimmerTool.SetStartFunction(() => {
                    Logger.Instance.LogInfo("Starting Screen Dimmer");
                    var config = ScreenDimmerConfig.Instance;
                    var manager = ScreenDimmerManager.Instance;
                    
                    // Load saved settings
                    Logger.Instance.LogDebug($"Loading Screen Dimmer settings - Brightness: {config.Brightness}, Method: {config.DimmingMethod}");
                    manager.Brightness = config.Brightness;
                    manager.Method = config.DimmingMethod;
                    
                    // Start the dimmer
                    manager.StartDimmer();
                    Logger.Instance.LogDebug("Screen Dimmer started successfully");
                    
                    // Subscribe to changes to save them
                    manager.BrightnessChanged += (s, brightness) => {
                        Logger.Instance.LogTrace($"Screen Dimmer brightness changed to: {brightness}");
                        config.Brightness = brightness;
                    };
                    manager.MethodChanged += (s, method) => {
                        Logger.Instance.LogTrace($"Screen Dimmer method changed to: {method}");
                        config.DimmingMethod = method;
                    };
                });
                screenDimmerTool.SetStopFunction(() => {
                    Logger.Instance.LogInfo("Stopping Screen Dimmer");
                    ScreenDimmerManager.Instance.StopDimmer();
                    Logger.Instance.LogDebug("Screen Dimmer stopped successfully");
                });
                screenDimmerTool.SetSettingsFunction(() => {
                    Logger.Instance.LogInfo("Opening Screen Dimmer settings window");
                    ScreenDimmerManager.Instance.ShowSettingsWindow();
                });
                screenDimmerTool.Enabled = ScreenDimmerConfig.Instance.Enabled;
                tools.Add(screenDimmerTool);
                Logger.Instance.LogTrace("Screen Dimmer tool added to tools list");

                // Load saved module states
                LoadModuleStates();
                Logger.Instance.LogInfo($"Tools initialization completed. Total tools: {tools.Count}");
            }
        }

        public List<ToolModule> GetTools()
        {
            lock (toolsLock)
            {
                return new List<ToolModule>(tools);
            }
        }

        public ToolModule? GetTool(int index)
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
            Logger.Instance.LogDebug($"Toggling tool at index: {index}");
            lock (toolsLock)
            {
                if (index >= 0 && index < tools.Count)
                {
                    var tool = tools[index];
                    var wasEnabled = tool.Enabled;
                    Logger.Instance.LogInfo($"Toggling tool '{tool.Name}' from {(wasEnabled ? "enabled" : "disabled")} to {(!wasEnabled ? "enabled" : "disabled")}");
                    
                    tool.Toggle();
                    
                    // Save ScreenDimmer enabled state to its config
                    if (tool.Name == "Screen Dimmer")
                    {
                        Logger.Instance.LogDebug($"Updating Screen Dimmer config enabled state to: {tool.Enabled}");
                        ScreenDimmerConfig.Instance.Enabled = tool.Enabled;
                    }
                    
                    SaveModuleStates();
                    Logger.Instance.LogDebug($"Tool '{tool.Name}' toggle completed successfully");
                }
                else
                {
                    Logger.Instance.LogWarning($"Invalid tool index for toggle: {index} (valid range: 0-{tools.Count - 1})");
                }
            }
        }

        public void AutoStartEnabledModules()
        {
            Logger.Instance.LogInfo("Auto-starting enabled modules...");
            lock (toolsLock)
            {
                var enabledTools = tools.Where(t => t.Enabled && !t.IsRunning).ToList();
                Logger.Instance.LogDebug($"Found {enabledTools.Count} enabled tools to start");
                
                foreach (var tool in enabledTools)
                {
                    try
                    {
                        Logger.Instance.LogInfo($"Auto-starting tool: {tool.Name}");
                        tool.Start();
                        Logger.Instance.LogDebug($"Tool '{tool.Name}' started successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogError($"Failed to auto-start tool '{tool.Name}'", ex);
                    }
                }
                Logger.Instance.LogInfo("Auto-start process completed");
            }
        }

        public void StopAllTools()
        {
            Logger.Instance.LogInfo("Stopping all running tools...");
            lock (toolsLock)
            {
                var runningTools = tools.Where(t => t.IsRunning).ToList();
                Logger.Instance.LogDebug($"Found {runningTools.Count} running tools to stop");
                
                foreach (var tool in runningTools)
                {
                    try
                    {
                        Logger.Instance.LogInfo($"Stopping tool: {tool.Name}");
                        tool.Stop();
                        Logger.Instance.LogDebug($"Tool '{tool.Name}' stopped successfully");
                    }
                    catch (Exception ex)
                    {
                        Logger.Instance.LogError($"Failed to stop tool '{tool.Name}'", ex);
                    }
                }
                Logger.Instance.LogInfo("Stop all tools process completed");
            }
        }

        private void SaveModuleStates()
        {
            try
            {
                Logger.Instance.LogDebug("Saving module states...");
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tool_manager_modules.ini");
                var lines = tools.Select(tool => $"{tool.Name}={tool.Enabled}").ToArray();
                File.WriteAllLines(configPath, lines);
                Logger.Instance.LogTrace($"Module states saved to: {configPath}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Failed to save module states", ex);
            }
        }

        private void LoadModuleStates()
        {
            try
            {
                Logger.Instance.LogDebug("Loading module states...");
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tool_manager_modules.ini");
                if (!File.Exists(configPath))
                {
                    Logger.Instance.LogInfo("Module states config file does not exist, using defaults");
                    return;
                }

                var moduleStates = new Dictionary<string, bool>();
                var lines = File.ReadAllLines(configPath);
                Logger.Instance.LogTrace($"Reading {lines.Length} lines from module states config");
                
                foreach (var line in lines)
                {
                    var parts = line.Split('=');
                    if (parts.Length == 2)
                    {
                        var enabled = parts[1] == "1" || parts[1].ToLower() == "true";
                        moduleStates[parts[0]] = enabled;
                        Logger.Instance.LogTrace($"Loaded state for '{parts[0]}': {enabled}");
                    }
                }

                // Apply states to tools (default to enabled if not found)
                foreach (var tool in tools)
                {
                    if (moduleStates.ContainsKey(tool.Name))
                    {
                        tool.Enabled = moduleStates[tool.Name];
                        Logger.Instance.LogDebug($"Applied saved state for '{tool.Name}': {tool.Enabled}");
                    }
                    else
                    {
                        tool.Enabled = true; // Default to enabled
                        Logger.Instance.LogDebug($"No saved state for '{tool.Name}', defaulting to enabled");
                    }
                }
                Logger.Instance.LogInfo("Module states loaded successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Failed to load module states", ex);
            }
        }
    }
}
