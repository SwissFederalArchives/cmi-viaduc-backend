using System;
using System.IO;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Engine.Asset;
using CMI.Engine.Asset.ParameterSettings;
using CMI.Engine.Asset.PreProcess;
using CMI.Manager.Asset.ParameterSettings;
using Serilog;

namespace CMI.Manager.Asset.TransformJp2ToPdfTester
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ConfigureLogging();

            Log.Information("CMI.Manager.Asset.TransformJp2ToPdfTester starting");

            if (args.Length == 0 || !Directory.Exists(args[0]))
            {
                Console.WriteLine(
                    "You need to provide a directory with a sample DIP package to process as an argument. As a second argument the JPEG quality can be provided. A third parameter sets the new size in percent of the original image");
                Console.ReadLine();
                return;
            }

            // Read source folder
            var sourceFolder = args[0];
            var jpegQuality = 80; // Default
            if (args.Length == 2 && int.TryParse(args[1], out var quality))
            {
                jpegQuality = quality;
            }

            var sizeInPercent = 100; // Default
            if (args.Length == 3 && int.TryParse(args[2], out var size))
            {
                sizeInPercent = size;
            }

            try
            {
                var transformEngine = new TransformEngine(new Xsl2Processor());
                ConvertAreldaMetadataXml(sourceFolder, transformEngine);

                var metadataFile = Path.Combine(sourceFolder, "header", "metadata.xml");
                var paket = (PaketDIP) Paket.LoadFromFile(metadataFile);
                var settings = new ScansZusammenfassenSettings { DefaultAufloesungInDpi = 300, GroesseInProzent = sizeInPercent, JpegQualitaetInProzent = jpegQuality };
                var scanProcessor = new ScanProcessor(new FileResolution(settings), settings);
                // Create pdf documents from scanned jpeg 2000 scans.
                scanProcessor.ConvertSingleJpeg2000ScansToPdfDocuments(paket, sourceFolder);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unexpected error. {ex.Message}");
            }
        }

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .ReadFrom.AppSettings().CreateLogger();
        }

        private static void ConvertAreldaMetadataXml(string tempFolder, ITransformEngine transformEngine)
        {
            Log.Information("Converting arelda metadata.xml file...");

            var metadataFile = Path.Combine(tempFolder, "header", "metadata.xml");
            var transformationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Html", "Xslt", "areldaConvert.xsl");

            // IF one of the files does not exist, log warning and create an "error" index.html file.
            if (!File.Exists(transformationFile) || !File.Exists(metadataFile))
            {
                throw new Exception(
                    $"Could not find the transformation file or the source file to transform. Make sure the both file exists.\nTransformation file: {transformationFile}\nSource file: {metadataFile}");
            }

            var result = transformEngine.TransformXml(metadataFile, transformationFile, null);
            File.WriteAllText(metadataFile, result);
            Log.Information("Converted.");
        }
    }
}