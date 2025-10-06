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
            try
            {
                settingsFunction?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing settings for {Name}: {ex.Message}");
            }
        }

        public void Start()
        {
            if (IsRunning || startFunction == null) return;

            try
            {
                startFunction.Invoke();
                IsRunning = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting {Name}: {ex.Message}");
                IsRunning = false;
            }
        }

        public void Stop()
        {
            if (!IsRunning) return;

            try
            {
                if (stopFunction != null)
                    stopFunction.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error stopping {Name}: {ex.Message}");
            }
            finally
            {
                IsRunning = false;
            }
        }

        public void Toggle()
        {
            Enabled = !Enabled;
            
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
