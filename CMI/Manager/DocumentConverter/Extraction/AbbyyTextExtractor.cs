using System;
using System.Collections.Generic;
using System.IO;
using CMI.Contract.DocumentConverter;
using CMI.Manager.DocumentConverter.Abbyy;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;
using Serilog;

namespace CMI.Manager.DocumentConverter.Extraction
{
    public class AbbyyTextExtractor : TextExtractorBase, INeedsAbbyyInstallation
    {
        private static readonly string[] extensions = { "tif", "tiff", "pdf", "jp2" };
        private readonly IAbbyyWorker abbyyWorker;

        private bool abbyPathExistsInSettings;
        private string pathToAbbyInstallation;

        public override IEnumerable<string> AllowedExtensions => extensions;

        public override int Rank => 1;

        public override bool IsAvailable => true;

        public string PathToAbbyyFrEngineDll
        {
            get => pathToAbbyInstallation;
            set
            {
                pathToAbbyInstallation = value;
                Log.Verbose($"Path to Abbyy FrEngine.dll has been set to '{pathToAbbyInstallation}'");
                abbyPathExistsInSettings = CheckForAbbyyInstallation();
            }
        }

        public bool PathToAbbyFrEngineDllHasBeenSet { get; set; }
        public string MissingAbbyyPathInstallationMessage { get; set; }
        public bool MissingAbbyyPathInstallationMessageHasBeenSet { get; set; }


        public AbbyyTextExtractor(IAbbyyWorker abbyyWorker)
        {
            this.abbyyWorker = abbyyWorker;
        }


        public override ExtractionResult ExtractText(IDoc doc, ITextExtractorSettings settings)
        {
            // Es wird von einer vorhandenen ABBYY-Installation ausgegangen.
            // Falls es keine Installation von ABBYY gibt, handelt es sich mit grosser Wahrscheinlichkeit um das Testsystem.
            // In diesem Fall wird ein Standard-Text zurückgegeben
            //
            if (!abbyPathExistsInSettings)
            {
                var result = new ExtractionResult(settings.MaxExtractionSize)
                {
                    HasError = true, 
                    ErrorMessage = $"{MissingAbbyyPathInstallationMessage} - {doc.FileName}"
                };
                return result;
            }

            try
            {
                var fs = doc.Stream as FileStream;
                var result = abbyyWorker.ExtractTextFromDocument(fs?.Name, settings);
                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Abbyy text extraction failed unexpectedly: {ex.Message}");
                throw;
            }
        }



        private bool CheckForAbbyyInstallation()
        {
            if (string.IsNullOrWhiteSpace(PathToAbbyyFrEngineDll))
            {
                throw new ArgumentException("Path to FrEngine.dll not set");
            }

            if (!File.Exists(PathToAbbyyFrEngineDll))
            {
                throw new FileNotFoundException($"Path/file '{PathToAbbyyFrEngineDll}' does not exist");
            }

            return true; // es ist noch keine Prüfung auf die Lizenz
        }
    }
}