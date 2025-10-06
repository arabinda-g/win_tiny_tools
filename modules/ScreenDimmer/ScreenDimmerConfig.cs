using System;
using System.IO;
using System.Collections.Generic;

namespace TinyTools.Modules.ScreenDimmer
{
    public class ScreenDimmerConfig
    {
        private static readonly Lazy<ScreenDimmerConfig> instance = new(() => new ScreenDimmerConfig());
        public static ScreenDimmerConfig Instance => instance.Value;

        private readonly string configFilePath;
        private readonly Dictionary<string, string> settings;

        // Configuration keys
        private const string KEY_BRIGHTNESS = "Brightness";
        private const string KEY_DIMMING_METHOD = "DimmingMethod";
        private const string KEY_ENABLED = "Enabled";

        // Default values
        public int DefaultBrightness => 100;
        public ScreenDimmerManager.DimmingMethod DefaultDimmingMethod => ScreenDimmerManager.DimmingMethod.Auto;
        public bool DefaultEnabled => false;

        // Properties
        public int Brightness
        {
            get => GetInt(KEY_BRIGHTNESS, DefaultBrightness);
            set => SetValue(KEY_BRIGHTNESS, value.ToString());
        }

        public ScreenDimmerManager.DimmingMethod DimmingMethod
        {
            get => GetEnum(KEY_DIMMING_METHOD, DefaultDimmingMethod);
            set => SetValue(KEY_DIMMING_METHOD, value.ToString());
        }

        public bool Enabled
        {
            get => GetBool(KEY_ENABLED, DefaultEnabled);
            set => SetValue(KEY_ENABLED, value.ToString());
        }

        private ScreenDimmerConfig()
        {
            // Create config file path
            var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            configFilePath = Path.Combine(baseDirectory, "screendimmer_settings.ini");
            
            settings = new Dictionary<string, string>();
            LoadSettings();
        }

        public void LoadSettings()
        {
            settings.Clear();

            if (!File.Exists(configFilePath))
            {
                // Create default config file
                SaveSettings();
                return;
            }

            try
            {
                var lines = File.ReadAllLines(configFilePath);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#") || line.StartsWith(";"))
                        continue;

                    var parts = line.Split(new char[] {'='}, 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        settings[key] = value;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading ScreenDimmer config: {ex.Message}");
                // Use defaults if loading fails
            }
        }

        public void SaveSettings()
        {
            try
            {
                var lines = new List<string>
                {
                    "# ScreenDimmer Module Configuration",
                    "# This file stores the settings for the ScreenDimmer module",
                    "#",
                    $"# Brightness: 10-100 (default: {DefaultBrightness})",
                    $"# DimmingMethod: GammaRamp, Overlay, Auto (default: {DefaultDimmingMethod})",
                    $"# Enabled: true/false (default: {DefaultEnabled})",
                    "",
                    $"{KEY_BRIGHTNESS}={Brightness}",
                    $"{KEY_DIMMING_METHOD}={DimmingMethod}",
                    $"{KEY_ENABLED}={Enabled}",
                    "",
                    $"# Last updated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}"
                };

                File.WriteAllLines(configFilePath, lines);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving ScreenDimmer config: {ex.Message}");
            }
        }

        private string GetValue(string key, string defaultValue)
        {
            return settings.TryGetValue(key, out var value) ? value : defaultValue;
        }

        private int GetInt(string key, int defaultValue)
        {
            var value = GetValue(key, defaultValue.ToString());
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        private bool GetBool(string key, bool defaultValue)
        {
            var value = GetValue(key, defaultValue.ToString());
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }

        private T GetEnum<T>(string key, T defaultValue) where T : struct, Enum
        {
            var value = GetValue(key, defaultValue.ToString());
            return Enum.TryParse<T>(value, out var result) ? result : defaultValue;
        }

        private void SetValue(string key, string value)
        {
            settings[key] = value;
            SaveSettings();
        }

        public void ResetToDefaults()
        {
            Brightness = DefaultBrightness;
            DimmingMethod = DefaultDimmingMethod;
            Enabled = DefaultEnabled;
        }

        public string GetConfigInfo()
        {
            return $"ScreenDimmer Config: Brightness={Brightness}%, Method={DimmingMethod}, Enabled={Enabled}";
        }
    }
}
