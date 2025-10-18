using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace TinyTools
{
    public class SettingsManager
    {
        private static readonly Lazy<SettingsManager> instance = new(() => new SettingsManager());
        public static SettingsManager Instance => instance.Value;

        private readonly Dictionary<string, bool> settings = new();
        private readonly Dictionary<string, string> stringSettings = new();
        private readonly string configPath;

        private SettingsManager()
        {
            configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tool_manager_settings.ini");
        }

        public void LoadSettings()
        {
            Logger.Instance.LogDebug("Loading application settings...");
            
            // Default settings
            settings["show_tray_icon"] = true;
            settings["minimize_to_tray"] = false;
            settings["start_minimized"] = false;
            settings["start_with_windows"] = false;
            
            // Default string settings
            stringSettings["log_level"] = "Off";

            if (!File.Exists(configPath))
            {
                Logger.Instance.LogInfo("Settings file does not exist, using defaults");
                return;
            }

            var lines = File.ReadAllLines(configPath);
            Logger.Instance.LogTrace($"Reading {lines.Length} lines from settings file: {configPath}");
            
            foreach (var line in lines)
            {
                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    
                    // Try to parse as boolean first
                    if (value == "1" || value.ToLower() == "true" || value.ToLower() == "false" || value == "0")
                    {
                        var boolValue = value == "1" || value.ToLower() == "true";
                        settings[key] = boolValue;
                        Logger.Instance.LogTrace($"Loaded boolean setting: {key} = {boolValue}");
                    }
                    else
                    {
                        // Store as string setting
                        stringSettings[key] = value;
                        Logger.Instance.LogTrace($"Loaded string setting: {key} = {value}");
                    }
                }
            }
            Logger.Instance.LogInfo("Application settings loaded successfully");
        }

        public void SaveSettings()
        {
            try
            {
                Logger.Instance.LogDebug("Saving application settings...");
                var lines = new List<string>();
                
                foreach (var pair in settings)
                {
                    lines.Add($"{pair.Key}={pair.Value}");
                    Logger.Instance.LogTrace($"Saving boolean setting: {pair.Key} = {pair.Value}");
                }
                
                foreach (var pair in stringSettings)
                {
                    lines.Add($"{pair.Key}={pair.Value}");
                    Logger.Instance.LogTrace($"Saving string setting: {pair.Key} = {pair.Value}");
                }
                
                File.WriteAllLines(configPath, lines);
                Logger.Instance.LogInfo($"Application settings saved successfully to: {configPath}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Failed to save application settings", ex);
            }
        }

        public bool GetSetting(string key)
        {
            return settings.ContainsKey(key) && settings[key];
        }

        public void SetSetting(string key, bool value)
        {
            settings[key] = value;
        }

        public string GetStringSetting(string key)
        {
            return stringSettings.ContainsKey(key) ? stringSettings[key] : string.Empty;
        }

        public void SetStringSetting(string key, string value)
        {
            stringSettings[key] = value;
        }

        public Dictionary<string, bool> GetAllSettings()
        {
            return new Dictionary<string, bool>(settings);
        }

        public Dictionary<string, string> GetAllStringSettings()
        {
            return new Dictionary<string, string>(stringSettings);
        }

        public void SetStartupRegistry(bool enable)
        {
            const string keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            const string valueName = "TinyTools";

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath, true))
                {
                    if (key != null)
                    {
                        if (enable)
                        {
                            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                            // For .NET Framework, use the assembly location directly
                            if (string.IsNullOrEmpty(exePath))
                            {
                                exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                            }
                            key.SetValue(valueName, exePath);
                        }
                        else
                        {
                            key.DeleteValue(valueName, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting startup registry: {ex.Message}");
            }
        }
    }
}
