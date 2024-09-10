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
      _logFilePath = Path.Combine(config.ExtractedFolderLocation, Program.LogFileName);

      try
      {
        // Ensure the directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath));
        Console.WriteLine($"Log directory created/verified: {Path.GetDirectoryName(_logFilePath)}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error creating log directory: {ex.Message}");
      }
    }

    public void Log(string message)
    {
      try
      {
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
        File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        Console.WriteLine($"Log entry written: {logEntry}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Failed to write to log file: {ex.Message}");
        Console.WriteLine($"Log file path: {_logFilePath}");
      }
    }
  }
}
