using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Aspose.Pdf;
using CMI.Contract.Common;
using CMI.Engine.Asset.ParameterSettings;
using CSJ2K;
using Serilog;
using Image = System.Drawing.Image;

namespace CMI.Engine.Asset.PreProcess
{
    public class PreProcessAnalyzerDetectAndFlagLargeDimensions : ProcessAnalyzerBase
    {
        private readonly FileResolution fileResolution;
        private readonly AssetPreparationSettings settings;
        public PreProcessAnalyzerDetectAndFlagLargeDimensions(FileResolution fileResolution, AssetPreparationSettings settings)
        {
            this.fileResolution = fileResolution;
            this.settings = settings;
        }

        protected override void AnalyzeFiles(string rootOrSubFolder, List<RepositoryFile> files)
        {
            foreach (var file in files)
            {
                var sourceFile = new FileInfo(Path.Combine(rootOrSubFolder, file.PhysicalName));
                if (sourceFile.Exists)
                {
                    Log.Information("FileName: {FullName}, detect and flag large dimensions", sourceFile.FullName);
                    switch (sourceFile.Extension.ToLower())
                    {
                        case ".pdf":
                            using (var pdfDocument = new Document(sourceFile.FullName))
                            {
                                if (!PdfManipulator.HasText(pdfDocument))
                                {
                                    file.SkipOCR = ShouldSkipPdf(pdfDocument, sourceFile.FullName);
                                }
                            }
                            break;
                        case ".jp2":
                            var decodedImage = J2kImage.FromFile(sourceFile.FullName);
                            var jp2Bitmap = decodedImage.As<Bitmap>();
                            TestImageDimension(file, jp2Bitmap, sourceFile);
                            break;
                        case ".tif":
                        case ".tiff":
                            var tiffBitmap = Image.FromFile(sourceFile.FullName);
                            TestImageDimension(file, tiffBitmap, sourceFile);
                            break;
                    }
                }
                else
                {
                    Log.Warning("Did not find file {FullName} while checking physical dimensions. ", sourceFile.FullName);
                }
            }
        }

        /// <summary>
        /// Tests if image dimension is larger than allowed.
        /// If yes, the file is marked for skipping
        /// </summary>
        private void TestImageDimension(RepositoryFile file, Image bitmap, FileInfo sourceFile)
        {
            file.SkipOCR = IsImageTooLarge(bitmap, sourceFile.FullName);
            if (file.SkipOCR)
            {
                Log.Information("Detected oversized image file. Skipping OCR recognition for file: {FullName}", sourceFile.FullName);
            }
        }

        private bool ShouldSkipPdf(Document pdfDocument, string path)
        {
            foreach (Page pdfPage in pdfDocument.Pages)
            {
                if (pdfPage?.Resources.Images.Count > 0)
                {
                    foreach (XImage image in pdfPage.Resources.Images)
                    {
                        var stream = new MemoryStream();
                        image.Save(stream);

                        var bitmap = Image.FromStream(stream);
                        if (IsImageTooLarge(bitmap, path))
                        {
                            Log.Information("Detected PDF with large dimension on page number {Number}. Skipping file: {FileName}", pdfPage.Number, pdfDocument.FileName);
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Here the width of the image is looked at.
        /// If the image is larger than the stored value (default A3),
        /// it is excluded from the font recognition because it will probably not find anything.
        /// </summary>
        /// <returns>True or false if it should skip or not</returns>
        private bool IsImageTooLarge(Image bitmap, string path)
        {
            var resolution = fileResolution.GetResolution(bitmap, path);
            var width = 25.4 * bitmap.Width / resolution;
            var height = 25.4 * bitmap.Height / resolution;
            return width > settings.SizeThreshold || height > settings.SizeThreshold;
        }
    }
}
