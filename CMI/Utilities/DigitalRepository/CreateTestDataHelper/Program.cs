using System;
using System.IO;
using System.Linq;
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
                .WriteTo.Console()
                .CreateLogger();

            // Get the AIP@DossierId data from the DB
            Log.Information("Get data from AIS...");
            var aipData = DbAccess.GetAipData();
            Log.Information($"Fetched {aipData.Count} records...");
            var repository = new CmisRepository();
            repository.Start();
            repository.ConnectToFirstRepository();
            
            // Argumente als text file übergeben
            if (File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UploadProperties.txt")))
            {
                string text = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UploadProperties.txt"));
                args = text.Split(' ');
            }
            if (CheckArguments(args, repository))
            {
                repository.CheckRootFolder();
                repository.StartDataUpload(aipData);
            }
            Console.WriteLine("Test data uploaded!!!");
        }

        private static bool CheckArguments(string[] args, CmisRepository repository)
        {
            var checkOk = false;
            switch (repository.CheckArguments(args))
            {
                case Arguments.IdImportError:
                    Log.Error("Die angegebene Id stimmt nicht" + Environment.NewLine
                                                               + "Rufen Sie die Hilfe auf mit -help");
                    break;
                case Arguments.DestinationPathNotExists:
                    Log.Error("Der Angegebene Dateipfad existiert nicht" + Environment.NewLine
                                                                          + "Rufen Sie die Hilfe auf mit -help");
                    break;
                case Arguments.Error:
                    Log.Error($"Die argumente sind nicht korrekt {args}" + Environment.NewLine
                                                                         + "Rufen Sie die Hilfe auf mit -help");
                    break;
                case Arguments.Unknow:
                    Log.Error(Environment.NewLine +
                              "Unbekanntes Argument: " + Environment.NewLine
                              + "Rufen Sie die Hilfe auf mit -help");
                    break;
                case Arguments.Help:
                    Log.Information(Environment.NewLine + 
                                    @"Beispiel eingabe -override true -path C:\Temp\PrimaryData -ids 30653567|1sck3r1m.5dm 306535717|mskyvlag.qwb" + Environment.NewLine +
                                    "(-o)verride:   Sollen schon vorhandene Einträge überschrieben werden.   Default: false " + Environment.NewLine +
                                    $"path:         Wo sind die gezippten Primärdaten                       Default: {repository.FileCopyDestinationPath} " + Environment.NewLine +
                                    $"ids:          Welche Dateien sollen aktualisiert werden (s. Beispiel) sonst werden alle Dateien mit einer zufälliger zip-Datei im Ordner {repository.FileCopyDestinationPath} hochgeladen" + Environment.NewLine);
                    break;
                case Arguments.Default:
                    checkOk = true;
                    Log.Information("Es werden die Default Einstellungen verwendet." + Environment.NewLine +
                                    "Vorhandene Daten werden nicht überschrieben" + Environment.NewLine +
                                    $"der Pfad für die Dateien zum hochladen ist {repository.FileCopyDestinationPath}");
                    break;
                case Arguments.UserConfig:
                    checkOk = true;
                    var textOverrideable = repository.OverrideFiles ? " " : " nicht ";
                    var textUpdateFiles = repository.OverrideFiles
                        ? "Es werden alle Datein aktualisiert"
                        : "Es werden alle nicht vorhandenen Datein erzeugt";
                    if (repository.OnlyThisIdsWithThisFileUpdate.Count > 0)
                    {
                        var textOverride = repository.OverrideFiles ? "Datein aktualisiert" : "nicht vorhandenen Datein erzeugt";
                        textUpdateFiles = $"Es werden Dateien mit folgenden Ids {string.Join(", ", repository.OnlyThisIdsWithThisFileUpdate.ToArray())} {textOverride}";
                    }

                    Log.Information(Environment.NewLine +
                                    "Es werden die Benutzer Einstellungen verwendet." + Environment.NewLine +
                                    $"Vorhandene Daten werden{textOverrideable}überschrieben" + Environment.NewLine +
                                    $"der Pfad für die Dateien zum hochladen ist {repository.FileCopyDestinationPath}" + Environment.NewLine +
                                    $"{textUpdateFiles}");
                    break;
            }

            return checkOk;
        }
    }
}