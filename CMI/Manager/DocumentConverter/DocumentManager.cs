using System;
using System.IO;
using System.Linq;
using CMI.Contract.DocumentConverter;
using CMI.Manager.DocumentConverter.Abbyy;
using CMI.Manager.DocumentConverter.Properties;
using CMI.Manager.DocumentConverter.Extraction;
using CMI.Manager.DocumentConverter.Extraction.Interfaces;
using CMI.Manager.DocumentConverter.Render;
using Serilog;

namespace CMI.Manager.DocumentConverter
{
    public interface IDocumentManager
    {
        /// <summary>
        ///     Gets the supported file types.
        /// </summary>
        /// <param name="processType">Type of the process.</param>
        string[] GetSupportedFileTypes(ProcessType processType);

        /// <summary>
        ///     Converts the specified file according to instructions in the conversion request.
        /// </summary>
        /// <param name="jobGuid">The job identifier.</param>
        /// <param name="sourceFile">The file to convert.</param>
        /// <param name="destinationExtension">The destination file type based on the extension.</param>
        /// <param name="videoQuality">The video quality if required</param>
        /// <param name="context">The context of the conversion</param>
        /// <returns>The converted file, or the same file if conversion is not supported or Abby is not installed.</returns>
        FileInfo Convert(string jobGuid, FileInfo sourceFile, string destinationExtension, string videoQuality, JobContext context);

        ExtractionResult ExtractText(string jobGuid, FileInfo sourceFile, JobContext context);

        int? GetPagesRemaining();

        bool TryOcrTextExtraction(out string text);
    }

    public class DocumentManager : IDocumentManager
    {
        private readonly ConverterInstallationInfo converterInfo;
        private readonly Extractor extractor;
        private readonly Renderer renderer;
        private readonly AbbyyLicense abbyyLicense;

        public DocumentManager(Extractor extractor, Renderer renderer, AbbyyLicense abbyyLicense, ConverterInstallationInfo converterInfo)
        {
            this.extractor = extractor;
            this.renderer = renderer;
            this.abbyyLicense = abbyyLicense;
            this.converterInfo = converterInfo;
        }

        public string[] GetSupportedFileTypes(ProcessType processType)
        {
            switch (processType)
            {
                case ProcessType.TextExtraction:
                    return extractor.GetAvailableExtensions().ToArray();
                case ProcessType.Rendering:
                    return renderer.GetAvailableExtensions().ToArray();
                default:
                    throw new ArgumentOutOfRangeException(nameof(processType), processType, null);
            }
        }

        public FileInfo Convert(string jobGuid, FileInfo sourceFile, string destinationExtension, string videoQuality, JobContext context)
        {
            if (!renderer.IsValidExtension(sourceFile.Extension))
            {
                throw new NotSupportedException($"Extension '{sourceFile.Extension}' is not supported for rendering");
            }

            var rendererForDestinationExtension = renderer.GetRendererForDestinationExtension(destinationExtension);

            if (rendererForDestinationExtension == null)
            {
                throw new NotSupportedException($"No file renderer for extension '{sourceFile.Extension}' found");
            }

            // If we have a conversion from pdf to pdf we only do an Abby/OCR conversion, if the pdf does not contain text already.
            using (var sourceDoc = new Doc(sourceFile, jobGuid))
            {
                if (MatchesExtension(sourceFile.Extension, "pdf") && MatchesExtension(destinationExtension, "pdf") && PdfHelper.HasText(sourceDoc))
                {
                    Log.Information("PDF already contains a text layer. No need for conversion. Returning source file.");
                    return sourceFile;
                }
            }

            SetProcessorPathByOutputExtension(rendererForDestinationExtension);

            // Es wird von einer vorhandenen ABBYY-Installation ausgegangen.
            // Falls es keine Installation von ABBYY gibt, handelt es sich mit grosser Wahrscheinlichkeit um das Testsystem.
            // In diesem Fall wird die Quelldatei zurückgegeben
            if (rendererForDestinationExtension is INeedsAbbyyInstallation)
            {
                if (string.IsNullOrWhiteSpace(DocumentConverterSettings.Default.PathToAbbyyFrEngineDll))
                {
                    var exception = new Exception("Path to FrEngine.dll not set, giving source file back");
                    Log.Warning(exception, exception.Message);
                    return sourceFile;
                }

                if (!File.Exists(DocumentConverterSettings.Default.PathToAbbyyFrEngineDll))
                {
                    throw new Exception($"Path/file '{DocumentConverterSettings.Default.PathToAbbyyFrEngineDll}' does not exist");
                }
            }

            var cmd = new RendererCommand
            {
                SourceFile = sourceFile,
                Identifier = jobGuid,
                VideoQuality = string.IsNullOrEmpty(videoQuality) ? VideoQuality.Default.ToString() : videoQuality,
                PdfTextLayerExtractionProfile = DocumentConverterSettings.Default.PDFTextLayerExtractionProfile,
                Context = context
            };

            Log.Information($"Start to render document {sourceFile} with job id {jobGuid} and extraction profile {cmd.PdfTextLayerExtractionProfile} and video quality {cmd.VideoQuality}");
            return rendererForDestinationExtension.Render(cmd);
        }

        public ExtractionResult ExtractText(string jobGuid, FileInfo sourceFile, JobContext context)
        {
            extractor.SetAbbyyInfosIfNeccessary(DocumentConverterSettings.Default.PathToAbbyyFrEngineDll, DocumentConverterSettings.Default.MissingAbbyyPathInstallationMessage);
            var extractorForExtension = extractor.GetExtractorForExtension(sourceFile.Extension);

            // If we have to extract text from a PDF, we check if the pdf does already contain text.
            using (var sourceDoc = new Doc(sourceFile, jobGuid))
            {
                if (MatchesExtension(sourceFile.Extension, "pdf") && PdfHelper.HasText(sourceDoc))
                {
                    Log.Information("PDF already contains a text layer. Using Aspose extractor.");
                    extractorForExtension = extractor.GetAsposeExtractor();
                }
            }

            if (extractorForExtension == null)
            {
                throw new Exception($"No extractor for extension '{sourceFile.Extension}' found");
            }

            using (var doc = new Doc(sourceFile, jobGuid))
            {
                var profile = DocumentConverterSettings.Default.OCRTextExtractionProfile;
                Log.Information($"Start to extract text from document {sourceFile} with job id {jobGuid} and extraction profile {profile}.");
                var result = extractorForExtension.ExtractText(doc, new DefaultTextExtractorSettings(profile) {Context = context});
                return result;
            }
        }

        public int? GetPagesRemaining()
        {
            try
            {
                return abbyyLicense.GetRemainingPages();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Exception on get remaining pages");
            }
            return null;
        }

        public bool TryOcrTextExtraction(out string text)
        {
            const string fileName = "AbbyyTiffTest.tif";

            var assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;
            var img = Resources.AbbyyTiffTest;
            var path = Path.Combine(assemblyLocation, fileName);
            var file = new FileInfo(path);

            if (!file.Exists)
            {
                img.Save(path);
                file.Refresh();
                if (!file.Exists)
                {
                    text = $"Unable to find file {file.FullName}";
                    return false;
                }
            }

            extractor.SetAbbyyInfosIfNeccessary(DocumentConverterSettings.Default.PathToAbbyyFrEngineDll, DocumentConverterSettings.Default.MissingAbbyyPathInstallationMessage);
            var abbyyExtractor = extractor.GetAbbyyExtractor();
            var result = abbyyExtractor.ExtractText(new Doc(file, Guid.NewGuid().ToString()), new DefaultTextExtractorSettings("TextExtraction_Speed"));

            if (result.HasError)
            {
                text = $"Could not extract text from sample file. ({result.ErrorMessage})";
                return false;
            }

            text = result.ToString();
            return true;
        }


        private void SetProcessorPathByOutputExtension(IRenderer targetRenderer)
        {
            switch (targetRenderer.OutputExtension.ToLowerInvariant())
            {
                case "mp3":
                    targetRenderer.ProcessorPath = converterInfo.PathToLameExe;
                    break;
                case "mp4":
                    targetRenderer.ProcessorPath = converterInfo.PathToFfMpegExe;
                    break;
                case "pdf":
                    break;
                default:
                    throw new ArgumentException($"Unsupported file extension '{targetRenderer.OutputExtension}'");
            }
        }


        private bool MatchesExtension(string extension, string extensionToMatch)
        {
            extension = extension.StartsWith(".") ? extension.Substring(1) : extension;
            extensionToMatch = extensionToMatch.StartsWith(".") ? extensionToMatch.Substring(1) : extensionToMatch;
            return extension.Equals(extensionToMatch, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}