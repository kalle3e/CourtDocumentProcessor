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
        static void Main(string[] args)
        {

            LoadConfiguration();

            Console.WriteLine(Configuration.ZipFilePath);

            var services = new ServiceCollection();
            ConfigureServices(services);

            using (ServiceProvider serviceProvider = services.BuildServiceProvider())
            {
                var processor = serviceProvider.GetService<ZipFileProcessor>();

                string zipFilePath = GetZipFilePath(args);
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
            string logPath = Path.Combine(Configuration.ExtractedFolderLocation, "application.log");
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
            string json = File.ReadAllText("AppSettings.json");
            Configuration = JsonConvert.DeserializeObject<AppSettings>(json);
        }
        private static string GetZipFilePath(string[] args)
        {
            if (args.Length > 0 && File.Exists(args[0]))
            {
                return args[0];
            }

            if (!string.IsNullOrEmpty(Configuration.ZipFilePath) && Directory.Exists(Configuration.ZipFilePath))
            {
                return Configuration.ZipFilePath;
            }

            Console.WriteLine("Please enter the path to the ZIP file:");
            string zipFilePath = Console.ReadLine();

            while (!File.Exists(zipFilePath))
            {
                Console.WriteLine("File not found. Please enter a valid path:");
                zipFilePath = Console.ReadLine();
            }

            return zipFilePath;
        }

        
    }
}

