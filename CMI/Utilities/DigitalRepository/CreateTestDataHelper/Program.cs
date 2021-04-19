using System;
using Serilog;

namespace CMI.Utilities.DigitalRepository.CreateTestDataHelper
{
    /// <summary>
    ///     Simple utility to create sample data in a CMIS compatible repository.
    /// </summary>
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Configure Logger
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .Enrich.FromLogContext()
                .WriteTo.LiterateConsole()
                .CreateLogger();

            var repository = new CmisRepository();
            repository.ConnectToFirstRepository();
            repository.CheckOrCreateRootFolder();


            // Get the AIP@DossierId data from the DB
            Log.Information("Get data from AIS...");
            var aipData = DbAccess.GetAipData();
            Log.Information($"Fetched {aipData.Count} records...");

            repository.CreateTestData(aipData);

            Console.WriteLine("Test data created!!!");
            Console.ReadLine();
        }
    }
}