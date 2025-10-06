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
        private readonly string configPath;

        private SettingsManager()
        {
            configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tool_manager_settings.ini");
        }

        public void LoadSettings()
        {
            // Default settings
            settings["show_tray_icon"] = true;
            settings["minimize_to_tray"] = false;
            settings["start_minimized"] = false;
            settings["start_with_windows"] = false;

            if (!File.Exists(configPath)) return;

            foreach (var line in File.ReadAllLines(configPath))
            {
                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();
                    settings[key] = value == "1" || value.ToLower() == "true";
                }
            }
        }

        public void SaveSettings()
        {
            var lines = new List<string>();
            foreach (var pair in settings)
            {
                lines.Add($"{pair.Key}={pair.Value}");
            }
            File.WriteAllLines(configPath, lines);
        }

        public bool GetSetting(string key)
        {
            return settings.ContainsKey(key) && settings[key];
        }

        public void SetSetting(string key, bool value)
        {
            settings[key] = value;
        }

        public Dictionary<string, bool> GetAllSettings()
        {
            return new Dictionary<string, bool>(settings);
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
