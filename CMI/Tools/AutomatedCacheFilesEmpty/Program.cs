using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac;
using CMI.Access.Common;
using CMI.Tools.AutomatedCacheFilesEmpty.Properties;
using CMI.Tools.AutomatedCacheFilesEmpty.Services;

// ReSharper disable LocalizableElement

namespace CMI.Tools.AutomatedCacheFilesEmpty
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: <program> <zip-file-directory> <x-days> [<mode> (test|prod)]");
                return;
            }

            string directoryPath = args[0];
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Invalid directory path: {directoryPath}");
                return;
            }

            if (!int.TryParse(args[1], out int xDays))
            {
                Console.WriteLine("Invalid number of days provided.");
                return;
            }

            string mode = args.Length >= 3 ? args[2].ToLower() : "test";
            if (mode != "test" && mode != "prod")
            {
                Console.WriteLine("Invalid mode. Use 'test' or 'prod'. Defaulting to 'test'.");
                mode = "test";
            }

            StartProgram(mode, directoryPath, xDays);
        }

        private static async Task StartProgram(string mode, string directoryPath, int xDays)
        {
            Console.WriteLine("Checking all files in mode: {0}", mode);
            if (mode != "prod")
            {
                Console.WriteLine("To actually delete the files older than x-days you need to provide 'prod' as cli argument...");
            }

            string connectionString = Settings.Default.connectionstring; 

            try
            {
                var builder = new ContainerBuilder();

                // Register dependencies
                builder.RegisterType<SearchIndexDataAccess>().As<ISearchIndexDataAccess>();
              
                // Register FileProcessor with the connectionString parameter
                builder.RegisterType<FileProcessor>()
                    .WithParameter("connectionString", connectionString)
                    .AsSelf();

                builder.RegisterType<ExcelReportGenerator>().AsSelf();

                var container = builder.Build();

                using (var scope = container.BeginLifetimeScope())
                {
                    var fileProcessor = scope.Resolve<FileProcessor>();
                    var results = await fileProcessor.ProcessFiles(directoryPath, xDays);

                    var reportGenerator = scope.Resolve<ExcelReportGenerator>();
                    string outputFile = Path.Combine(directoryPath, "CacheFilesReport.xlsx");
                    reportGenerator.GenerateReport(results, outputFile);

                    if (mode == "prod")
                    {
                        // Delete files listed in results
                        foreach (var result in results.Where(r => r.ToBeDeleted))
                        {
                            File.Delete(result.FilePath);
                        }
                        Console.WriteLine("All unneeded files deleted.");
                    }

                    Console.WriteLine($"Report generated: {outputFile}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
