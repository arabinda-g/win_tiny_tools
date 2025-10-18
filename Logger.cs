using System;
using System.IO;
using System.Threading;

namespace TinyTools
{
    public enum LogLevel
    {
        Off = 0,
        Error = 1,
        Warning = 2,
        Info = 3,
        Debug = 4,
        Trace = 5
    }

    public class Logger
    {
        private static readonly Lazy<Logger> instance = new(() => new Logger());
        public static Logger Instance => instance.Value;

        private readonly object lockObject = new();
        private readonly string logFilePath;
        private LogLevel currentLogLevel = LogLevel.Off;

        private Logger()
        {
            var logFileName = $"TinyTools_{DateTime.Now:yyyyMMdd}.log";
            logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, logFileName);
        }

        public LogLevel LogLevel
        {
            get => currentLogLevel;
            set
            {
                currentLogLevel = value;
                LogInfo($"Log level changed to: {value}");
            }
        }

        public void LogError(string message, Exception? ex = null)
        {
            if (currentLogLevel >= LogLevel.Error)
            {
                WriteLog("ERROR", message, ex);
            }
        }

        public void LogWarning(string message)
        {
            if (currentLogLevel >= LogLevel.Warning)
            {
                WriteLog("WARN", message);
            }
        }

        public void LogInfo(string message)
        {
            if (currentLogLevel >= LogLevel.Info)
            {
                WriteLog("INFO", message);
            }
        }

        public void LogDebug(string message)
        {
            if (currentLogLevel >= LogLevel.Debug)
            {
                WriteLog("DEBUG", message);
            }
        }

        public void LogTrace(string message)
        {
            if (currentLogLevel >= LogLevel.Trace)
            {
                WriteLog("TRACE", message);
            }
        }

        private void WriteLog(string level, string message, Exception? ex = null)
        {
            lock (lockObject)
            {
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var threadId = Thread.CurrentThread.ManagedThreadId;
                    var logEntry = $"[{timestamp}] [{level}] [T{threadId:D3}] {message}";
                    
                    if (ex != null)
                    {
                        logEntry += $"{Environment.NewLine}Exception: {ex}";
                    }

                    File.AppendAllText(logFilePath, logEntry + Environment.NewLine);
                }
                catch
                {
                    // Silently fail to avoid infinite loops
                }
            }
        }

        public string GetLogFilePath()
        {
            return logFilePath;
        }

        public void ClearLog()
        {
            lock (lockObject)
            {
                try
                {
                    if (File.Exists(logFilePath))
                    {
                        File.Delete(logFilePath);
                    }
                    LogInfo("Log file cleared");
                }
                catch (Exception ex)
                {
                    LogError("Failed to clear log file", ex);
                }
            }
        }
    }
}
