using System;
using System.IO;
using CMI.Contract.Common.Gebrauchskopie;

namespace CMI.Engine.Asset.ConsoleTests
{
    /// <summary>
    ///     Simple testing for xslt conversions
    /// </summary>
    internal class Program
    {
        private static TransformEngine transformEngine;

        private static void Main(string[] args)
        {
            var sourceFile = args[0];
            if (!File.Exists(sourceFile))
            {
                Console.WriteLine($"File {sourceFile} not found. Enter source file as first command line argument");
                return;
            }

            transformEngine = new TransformEngine(new Xsl2Processor());
            ConvertAreldaMetadataXml(sourceFile);
        }

        private static void ConvertAreldaMetadataXml(string sourceFile)
        {
            // Get Metadata xml
            var transformationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Html", "Xslt", "areldaConvert.xsl");

            // IF one of the files does not exist, log warning and create an "error" index.html file.
            if (!File.Exists(transformationFile) || !File.Exists(sourceFile))
            {
                return;
            }

            // Do transformation
            var result = transformEngine.TransformXml(sourceFile, transformationFile, null);
            var tempFile = Path.GetTempFileName();
            File.WriteAllText(tempFile, result);

            var paket = (PaketDIP) Paket.LoadFromFile(tempFile);


            Console.WriteLine($"Paket generiert am: {paket.Generierungsdatum.ToShortDateString()}");
            Console.ReadLine();
        }
    }
}