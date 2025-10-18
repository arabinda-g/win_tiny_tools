using System;
using System.Threading;

namespace TinyTools
{
    public class ToolModule
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public bool Enabled { get; set; }
        public bool IsRunning { get; private set; }

        private Action startFunction;
        private Action stopFunction;
        private Action settingsFunction;

        public ToolModule(string name, string description)
        {
            Name = name;
            Description = description;
            Enabled = false;
            IsRunning = false;
            Logger.Instance.LogTrace($"ToolModule created: {name} - {description}");
        }

        public void SetStartFunction(Action startFunc)
        {
            startFunction = startFunc;
        }

        public void SetStopFunction(Action stopFunc)
        {
            stopFunction = stopFunc;
        }

        public void SetSettingsFunction(Action settingsFunc)
        {
            settingsFunction = settingsFunc;
        }

        public bool HasSettings => settingsFunction != null;

        public void ShowSettings()
        {
            Logger.Instance.LogDebug($"Showing settings for tool: {Name}");
            try
            {
                settingsFunction?.Invoke();
                Logger.Instance.LogTrace($"Settings function invoked successfully for: {Name}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error showing settings for {Name}", ex);
                Console.WriteLine($"Error showing settings for {Name}: {ex.Message}");
            }
        }

        public void Start()
        {
            if (IsRunning)
            {
                Logger.Instance.LogWarning($"Attempted to start already running tool: {Name}");
                return;
            }
            
            if (startFunction == null)
            {
                Logger.Instance.LogWarning($"No start function defined for tool: {Name}");
                return;
            }

            Logger.Instance.LogDebug($"Starting tool: {Name}");
            try
            {
                startFunction.Invoke();
                IsRunning = true;
                Logger.Instance.LogInfo($"Tool started successfully: {Name}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error starting {Name}", ex);
                Console.WriteLine($"Error starting {Name}: {ex.Message}");
                IsRunning = false;
            }
        }

        public void Stop()
        {
            if (!IsRunning)
            {
                Logger.Instance.LogDebug($"Attempted to stop already stopped tool: {Name}");
                return;
            }

            Logger.Instance.LogDebug($"Stopping tool: {Name}");
            try
            {
                if (stopFunction != null)
                {
                    stopFunction.Invoke();
                    Logger.Instance.LogTrace($"Stop function invoked for: {Name}");
                }
                else
                {
                    Logger.Instance.LogDebug($"No stop function defined for tool: {Name}");
                }
                Logger.Instance.LogInfo($"Tool stopped successfully: {Name}");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error stopping {Name}", ex);
                Console.WriteLine($"Error stopping {Name}: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
            }
        }

        public void Toggle()
        {
            var oldEnabled = Enabled;
            Enabled = !Enabled;
            Logger.Instance.LogDebug($"Toggling tool '{Name}' from {oldEnabled} to {Enabled}");
            
            if (Enabled)
            {
                Start();
            }
            else
            {
                Stop();
            }
        }
    }
}
