using System;
using System.IO;
using Autofac;
using CMI.Utilities.DigitalRepository.PrimaryDataHarvester.Properties;
using MassTransit;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Utilities.DigitalRepository.PrimaryDataHarvester
{
    /// <summary>
    ///     Simple utility to create sample data in a CMIS compatible repository.
    /// </summary>
    internal class Program
    {
        private static IBusControl bus;

        private static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();

            Log.Information("service is starting");

            var builder = ContainerConfigurator.Configure();

            var container = builder.Build();

            bus = container.Resolve<IBusControl>();
            bus.Start();

            Log.Information("service started");
            var veIds = ConfigurationRead("archiveRecordIdOrSignature.json");
            // Loop through every entry in the list.
            foreach (var veId in veIds.RecordIdOrSig)
            {
                var harvester = container.Resolve<PrimaryDataHarvester>();
                Log.Information($"Get data from Repository {veId}");

                harvester.Start(veId);
            }

            Log.Debug("Daten wurden geholt!!!");
            Console.ReadLine();
        }

        private static ArchiveRecordIdOrSignature ConfigurationRead(string configurationFile)
        {
            configurationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configurationFile);
            if (File.Exists(configurationFile))
            {
                var json = File.ReadAllText(configurationFile);
                return JsonConvert.DeserializeObject<ArchiveRecordIdOrSignature>(json);
            }

            throw new FileNotFoundException($"could not find the configuration file {configurationFile}.");
        }
    }
}