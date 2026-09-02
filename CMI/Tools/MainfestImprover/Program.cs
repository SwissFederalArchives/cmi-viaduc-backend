using CommandLine;
using Serilog;
using System;
using System.IO;
using System.Linq;

namespace CMI.Tools.ManifestImprover
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ConfigureLogging();

            var directoryName = "";
            var pattern = "";
            var substitution = "";
            var isTestRun = true;
            var errors = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    directoryName = o.DirectoryName;
                    pattern = o.Pattern;
                    substitution = o.Substitution;
                    bool.TryParse(o.IsTestRun, out isTestRun);
                    
                }).Errors;

            if (errors.FirstOrDefault() == null && Directory.Exists(directoryName))
            {
                Log.Information("#########################################################################");
                Log.Information("#########################################################################");

                Log.Information("Directory: " + directoryName);
                Log.Information("Pattern: " + pattern);
                Log.Information("Substitution: " + substitution);
                Log.Information("Is test run: " + isTestRun);

                try
                {
                    using var improver = new ManifestImprover();
                    improver.Improve(directoryName, pattern, substitution, isTestRun);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error during improve process. Aborting...");
                }
            }
            else if (errors.FirstOrDefault() != null)
            {
                Log.Error("Argument Fehler: " + errors.FirstOrDefault().Tag);
            }
            else
            {
                Log.Error("Argument ist kein Verzeichnis: " + directoryName);
            }


            Console.ReadLine();

        }

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .WriteTo.Console()
                .CreateLogger();
        }
    }

    internal class Options
    {
        [Option('d', "directoryName", Required = true, HelpText = "The directoryName whose start to search files")]
        public string DirectoryName { get; set; }
        [Option('p', "pattern", Required = true, Default = @"(https:\/\/(www|image)\.recherche\.bar\.admin\.ch\/(recherche|iiif)\/.*?\/)(\/){1,6}", HelpText = "The pattern")]
        public string Pattern { get; set; }

        [Option('s', "substitution", Default = "$1", Required = true, HelpText = "The substitution")]
        public string Substitution { get; set; }

        [Option('t', "testRun", Default = true, Required = true, HelpText = "if the replacement should be made or not. 0 or false means, we are actually changing data")]
        public string IsTestRun { get; set; }
    }
}
