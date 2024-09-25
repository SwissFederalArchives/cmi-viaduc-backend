using CommandLine;
using System.IO;
using System;
using System.Linq;

namespace CMI.Tools.ManifestImprover
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var directoryName = "";
            var pattern = "";
            var substitution = "";
            var errors = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    directoryName = o.DirectoryName;
                    pattern = o.Pattern;
                    substitution = o.Substitution;
                }).Errors;

            if (errors.FirstOrDefault() == null && Directory.Exists(directoryName))
            {
                Console.WriteLine("Directory: " + directoryName);
                Console.WriteLine("Pattern: " + pattern);
                Console.WriteLine("Substitution: " + substitution);

                using var improver = new ManifestImprover();
                improver.Improve(directoryName, pattern, substitution);
            }
            else if (errors.FirstOrDefault() != null)
            {
                Console.WriteLine("Argument Fehler: " + errors.FirstOrDefault().Tag);
            }
            else
            {
                Console.WriteLine("Argument ist kein Verzeichnis: " + directoryName);
            }


            Console.ReadLine();

        }
    }

    internal class Options
    {
        [Option('d', "directoryName", Required = true, HelpText = "The directoryName whose start to search files")]
        public string DirectoryName { get; set; }
        [Option('p', "pattern", Required = false, Default = @"(https:\/\/viaducdev\.cmiag\.ch\/image\/.*?%2F)(%2F){1,6}(.*?\.jpg)",  HelpText = "The pattern")]
        public string Pattern { get; set; }

        [Option('s', "substitution", Default = "$1$3", Required = false, HelpText = "The substitution")]
        public string Substitution { get; set; }
    }
}
