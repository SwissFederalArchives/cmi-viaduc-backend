using System.Collections.Generic;
using System.IO;
using Aspose.Pdf;
using Aspose.Pdf.Optimization;
using CMI.Contract.Common;
using CMI.Engine.Asset.ParameterSettings;
using Serilog;
using Image = System.Drawing.Image;

namespace CMI.Engine.Asset.PreProcess
{
    public class PreProcessAnalyzerOptimizePdf : PreProcessAnalyzer
    {
        public PreProcessAnalyzerOptimizePdf(FileResolution fileResolution, AssetPreparationSettings settings) : base(fileResolution, settings)
        {
        }

        protected override void AnalyzeFiles(string tempFolder, List<RepositoryFile> files)
        {
            foreach (var file in files)
            {
                var sourceFile = new FileInfo(Path.Combine(tempFolder, file.PhysicalName));
                if (sourceFile.Exists)
                {
                    Log.Information("FileName: {FullName}, Detect suspicious file sizes", sourceFile.FullName);
                    switch (sourceFile.Extension.ToLower())
                    {
                        case ".pdf":
                            using (var pdfDocument = new Document(sourceFile.FullName))
                            {
                                if (ShouldPdfOptimized(pdfDocument, sourceFile.FullName))
                                {
                                    Log.Information("File {FullName} will be optimized as there are too many big images (storage size).",
                                        sourceFile.FullName);
                                    // Optimize
                                    var optimizationOptions = new OptimizationOptions
                                    {
                                        ImageCompressionOptions =
                                        {
                                            CompressImages = true,
                                            ImageQuality = settings.OptimizedQualityInPercent,
                                            ResizeImages = true
                                        }
                                    };

                                    pdfDocument.OptimizeResources(optimizationOptions);
                                    pdfDocument.Save(sourceFile.FullName);
                                }
                            }
                            break;
                    }
                }
                else
                {
                    Log.Warning("Did not find file {FullName} while checking storage needs. ", sourceFile.FullName);
                }
            }
        }
        
        private bool ShouldPdfOptimized(Document pdfDocument, string path)
        {
            var imagesTooBigCount = 0;
            foreach (Page pdfPage in pdfDocument.Pages)
            {
                // Only do the check if there is exactly one image on the page (scans)
                if (pdfPage?.Resources.Images.Count == 1)
                {
                    XImage image = pdfPage.Resources.Images[1];
                    var stream = new MemoryStream();
                    image.Save(stream);

                    var bitmap = Image.FromStream(stream);
                    if (IsImageTooBig(bitmap, stream.Length, path))
                    {
                        imagesTooBigCount++;

                        // Check if we already skipped the threshold
                        if (imagesTooBigCount * 100.0 / pdfDocument.Pages.Count > settings.AllowTooBigPercentage)
                        {
                            return true;
                        }
                    }
                }
                
            }
           
            // if we come here, we don't enough images that are too big
            return false;
        }

        private bool IsImageTooBig(Image bitmap, long sizeInByte, string path)
        {
            var resolution = fileResolution.GetResolution(bitmap, path);
            // in cm
            var width = 2.54 *bitmap.Width / resolution;
            var height = 2.54 * bitmap.Height / resolution;

            // Divide the area of the image by the area of the reference A4 image
            var percentFromA4Width = width * height / (21 * 29.7);
            // MaxSizePerA4 is in KB
            return sizeInByte / percentFromA4Width > settings.MaxSizePerA4 * 1000;
        }
    }
}
