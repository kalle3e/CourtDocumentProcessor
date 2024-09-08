using System;
using System.IO;
using CourtDocumentProcessor.Interfaces;

namespace CourtDocumentProcessor
{
    public class FileLogger : ILogger
    {
        private readonly string _logFilePath;

        public FileLogger(AppSettings config)
        {
            _logFilePath = Path.Combine(config.ExtractedFolderLocation, "log.txt");
        }

        public void Log(string message)
        {
            try
            {
                string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // In a real-world scenario, you might want to handle this exception more gracefully
                Console.WriteLine($"Failed to write to log file: {ex.Message}");
            }
        }
    }
}