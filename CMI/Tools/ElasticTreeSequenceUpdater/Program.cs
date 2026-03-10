using System;
using System.Threading.Tasks;
using Serilog;
using CMI.Tools.ElasticTreeSequenceUpdater.Properties;
using CMI.Tools.ElasticTreeSequenceUpdater.Services;

namespace CMI.Tools.ElasticTreeSequenceUpdater
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var logFile = Settings.Default.LogFilePath;

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(logFile, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 30)
                .CreateLogger();

            try
            {
                Log.Information("Starting SynchronisationService...");
                var service = new SynchronisationService();
                await service.RunAsync(args);
                Log.Information("Service finished successfully.");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Service terminated unexpectedly.");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
