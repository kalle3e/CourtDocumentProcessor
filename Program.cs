using System;
using System.IO;
using System.IO.Compression;
using System.Xml;
using System.Xml.Schema;
using System.Net.Mail;
using System.Collections.Generic;
using System.Configuration;
using CourtDocumentProcessor.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
//using Microsoft.Bcl.AsyncInterfaces;

namespace CourtDocumentProcessor
{
  class Program
  {
    private static AppSettings Configuration { get; set; }
    public const string LogFileName = "Application.log";
    static void Main(string[] args)
    {

      LoadConfiguration();

      Console.WriteLine(Configuration.ZipFilePath);

      var services = new ServiceCollection();
      ConfigureServices(services);

      using (ServiceProvider serviceProvider = services.BuildServiceProvider())
      {
        var logger = serviceProvider.GetRequiredService<ILogger>();
        logger.Log("Application started");
        logger.Log($"Log file should be created at: {Path.Combine(Configuration.ExtractedFolderLocation, LogFileName)}");


        var processor = serviceProvider.GetService<ZipFileProcessor>();

        string zipFilePath = GetZipFilePath();
        processor.ProcessZipFile(zipFilePath);
      }

      Console.WriteLine("Processing complete. Press any key to view the log, or 'q' to quit.");
      if (Console.ReadKey().Key != ConsoleKey.Q)
      {
        DisplayLog();
      }
    }

    static void DisplayLog()
    {
      string logPath = Path.Combine(Configuration.ExtractedFolderLocation, "LogFileName");
      if (File.Exists(logPath))
      {
        Console.Clear();
        Console.WriteLine("Application Log:");
        Console.WriteLine(new string('-', 50));
        Console.WriteLine(File.ReadAllText(logPath));
        Console.WriteLine(new string('-', 50));
        Console.WriteLine("End of log. Press any key to exit.");
        Console.ReadKey();
      }
      else
      {
        Console.WriteLine("Log file not found.");
        Console.ReadKey();
      }

    }

    private static void ConfigureServices(IServiceCollection services)
    {
      services.AddSingleton(Configuration);
      services.AddSingleton<CourtDocumentProcessor.Interfaces.IAppConfigManager>(sp => new AppConfigManager(Configuration));
      services.AddTransient<ZipFileProcessor>();
      services.AddTransient<ILogger, FileLogger>();
      services.AddTransient<IEmailNotifier, EmailNotifier>();
    }


    private static void LoadConfiguration()
    {
      try
      {
        string json = File.ReadAllText("appsettings.json");
        Configuration = JsonConvert.DeserializeObject<AppSettings>(json);
        Console.WriteLine($"Configuration loaded successfully. ZipFilePath: {Configuration.ZipFilePath}");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error loading configuration: {ex.Message}");
        Configuration = new AppSettings(); // Initialize with default values
      }
    }
    private static string GetZipFilePath()
    {
      if (!string.IsNullOrEmpty(Configuration.ZipFilePath) && File.Exists(Configuration.ZipFilePath))
      {
        Console.WriteLine($"Using ZIP file path from configuration: {Configuration.ZipFilePath}");
        return Configuration.ZipFilePath;
      }

      Console.WriteLine("ZIP file path not found in configuration or file does not exist.");
      Console.WriteLine("Please enter the path to the ZIP file:");

      string zipFilePath;
      do
      {
        zipFilePath = Console.ReadLine();
        if (File.Exists(zipFilePath))
        {
          return zipFilePath;
        }
        Console.WriteLine("File not found. Please enter a valid path:");
      } while (true);
    }
  }
}

