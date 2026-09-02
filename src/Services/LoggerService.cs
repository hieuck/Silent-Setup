using System.IO;

namespace SilentSetup.Services
{
    /// <summary>
    /// Simple file-based logger
    /// </summary>
    public class LoggerService
    {
        private readonly string _logDirectory;
        private readonly object _lockObject = new();

        public LoggerService(string logDirectory = "logs")
        {
            // Use exe directory for portable app
            var exeDir = AppContext.BaseDirectory;
            _logDirectory = Path.Combine(exeDir, logDirectory);
            Directory.CreateDirectory(_logDirectory);
        }

        public void Log(string level, string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var logMessage = $"{timestamp} [{level}] {message}";

            lock (_lockObject)
            {
                // Write to daily log file
                var logFile = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
                File.AppendAllText(logFile, logMessage + Environment.NewLine);
            }
        }

        public void Info(string message) => Log("INFO", message);
        public void Warn(string message) => Log("WARN", message);
        public void Error(string message) => Log("ERROR", message);

        public List<string> GetTodayLogs()
        {
            var logFile = Path.Combine(_logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
            if (File.Exists(logFile))
            {
                return File.ReadAllLines(logFile).ToList();
            }
            return new List<string>();
        }

        public List<string> GetLogFiles()
        {
            return Directory.GetFiles(_logDirectory, "*.log")
                .OrderByDescending(f => f)
                .ToList();
        }
    }
}
