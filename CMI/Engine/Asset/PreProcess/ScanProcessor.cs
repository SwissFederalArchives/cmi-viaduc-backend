﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using Aspose.Pdf;
using Aspose.Pdf.Operators;
using Aspose.Pdf.Optimization;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Engine.Asset.ParameterSettings;
using Serilog;
using Matrix = Aspose.Pdf.Matrix;
using Rectangle = Aspose.Pdf.Rectangle;

namespace CMI.Engine.Asset.PreProcess
{
    public class ScanProcessor : IScanProcessor
    {
        private readonly ScansZusammenfassenSettings settings;
        private PaketDIP paketToConvert;
        private string rootFolder;
        private readonly ImageHelper imageHelper;

        public ScanProcessor(ImageHelper imageHelper, ScansZusammenfassenSettings settings)
        {
            try
            {
                var licensePdf = new License();
                licensePdf.SetLicense("Aspose.Total.NET.lic");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while setting Aspose license.");
                throw;
            }

            this.imageHelper = imageHelper;
            this.settings = settings;
        }

        /// <summary>
        ///     <para>
        ///         Converts single page jpeg2000 Scans found within the package into (multi-paged) pdf document.
        ///         Per document or dossier (with direct dateiRef's) one pdf is created. The metadata information in the package is
        ///         updated to reflect the changes made.
        ///     </para>
        ///     <para>The following assumptions are made:</para>
        ///     <list type="bullet">
        ///         <item>JPEG 2000 Files have the extension .jp2</item>
        ///         <item>The .jp2 may be accompanied by a premis xml file. The premis filename is "[jpeg200Filename]_premis.xml</item>
        ///         <item>
        ///             Within one document or (dossier with dateiRef) only .jp2 files are allowed.
        ///             If other file types are mixed in, (except for the premis files) the conversion silently fails for that
        ///             document.
        ///         </item>
        ///         <item>The premis files are removed after the pdf creation took place.</item>
        ///     </list>
        /// </summary>
        /// <param name="paket">The package to be converted</param>
        /// <param name="folder">The root folder where the files can be found.</param>
        public void ConvertSingleJpeg2000ScansToPdfDocuments(PaketDIP paket, string folder)
        {
            rootFolder = folder;
            paketToConvert = paket;

            foreach (var ordnungssystemposition in paket.Ablieferung.Ordnungssystem.Ordnungssystemposition)
            {
                ProcessOrdnungssystemPosition(ordnungssystemposition);
            }
        }

        private void ProcessOrdnungssystemPosition(OrdnungssystempositionDIP ordnungssystemposition)
        {
            foreach (var dossier in ordnungssystemposition.Dossier)
            {
                ProcessDossier(dossier);
            }

            foreach (var ordnungssystemSubPosition in ordnungssystemposition.Ordnungssystemposition)
            {
                ProcessOrdnungssystemPosition(ordnungssystemSubPosition);
            }
        }

        private void ProcessDossier(DossierDIP dossier)
        {
            // A dossier can have attached files, without having a document
            ProcessDateiRefList(dossier.DateiRef);

            foreach (var dokument in dossier.Dokument)
            {
                ProcessDocuments(dokument);
            }

            foreach (var subDossier in dossier.Dossier)
            {
                ProcessDossier(subDossier);
            }
        }

        private void ProcessDocuments(DokumentDIP dokument)
        {
            Log.Verbose("Checking document {titel} for JP2 files", dokument.Titel);
            ProcessDateiRefList(dokument.DateiRef);
        }

        private void ProcessDateiRefList(IList<string> dateiRefList)
        {
            var filesForConversion = new List<FileInfo>();
            var ordner = paketToConvert.Inhaltsverzeichnis.Ordner;

            // Get the corresponding files from the content
            var files = dateiRefList.Select(dateiRef =>
            {
                var file = GetDatei(dateiRef, ordner);
                if (file == null)
                {
                    Log.Error("Im metadata.xml fehlt die Datei mit DateiRef '{dateiRef}'", dateiRef);
                    throw new InvalidOperationException($"Im metadata.xml fehlt die Datei mit DateiRef '{dateiRef}'");
                }

                return file;
            }).ToList();

            // Check if list contains only jp2 files, plus the corresponding premis files
            var jp2Files = files.Where(f => f.Name.EndsWith(".jp2")).ToList();
            var premisFiles = jp2Files.Select(j => files.FirstOrDefault(
                    f => f.Name.Equals(j.Name.Remove(j.Name.Length - ".jp2".Length) + "_premis.xml", StringComparison.InvariantCultureIgnoreCase)))
                .Where(f => f != null).ToList();

            // Validation: Only jp2 files and accompaning premis files are allowed.
            // So the count must add up to the total number of files
            if (premisFiles.Count + jp2Files.Count < files.Count)
            {
                return;
            }

            foreach (var file in jp2Files)
            {
                var path = GetPath(file.Id, ordner, rootFolder);
                var fileName = Path.Combine(path, file.Name);

                filesForConversion.Add(new FileInfo(fileName));
            }

            if (filesForConversion.Count > 0)
            {
                Log.Information("Converting {count} JP2 files to pdf document...", filesForConversion.Count);


                var outputFile = $"{DateTime.Now.Ticks}.pdf";
                // ReSharper disable once AssignNullToNotNullAttribute
                var outputFileWithPath = Path.Combine(filesForConversion[0].DirectoryName, outputFile);

                CreatePdfFile(filesForConversion, outputFileWithPath);
                DeletePremisFiles(premisFiles);
            }
        }

        private void CreatePdfFile(List<FileInfo> imageFileList, string pdfFileName)
        {
            var pdfDocument = new Document();
            var parents = new DateiParents();

            foreach (var imageFile in imageFileList)
            {
                AddPage(pdfDocument, imageFile.FullName);
                pdfDocument.FreeMemory();

                parents = MetadataXmlUpdater.RemoveFile(imageFile, paketToConvert, rootFolder);
                imageFile.Delete();
            }

            // Optimize
            var optimizationOptions = new OptimizationOptions
            {
                ImageCompressionOptions =
                {
                    CompressImages = true,
                    ImageQuality = settings.JpegQualitaetInProzent,
                    ResizeImages = true
                }
            };

            pdfDocument.OptimizeResources(optimizationOptions);
            pdfDocument.Save(pdfFileName);
            MetadataXmlUpdater.AddFile(new FileInfo(pdfFileName), parents);
        }

        private void AddPage(Document pdfDocument, string filePath)
        {
            var imageInfo = GetImageInfoEx(filePath);
            var page = pdfDocument.Pages.Add();

            // Resize page, or we have default A4 Size
            page.SetPageSize(imageInfo.PageSize.Width, imageInfo.PageSize.Height);

            // add image to Images collection of Page Resources
            page.Resources.Images.Add(imageInfo.Stream);
            // using GSave operator: this operator saves current graphics state
            page.Contents.Add(new GSave());

            // create Rectangle and Matrix objects
            var rectangle = new Rectangle(0, 0, imageInfo.PageSize.Width, imageInfo.PageSize.Height);
            var matrix = new Matrix(new[] { rectangle.URX - rectangle.LLX, 0, 0, rectangle.URY - rectangle.LLY, rectangle.LLX, rectangle.LLY });

            // using ConcatenateMatrix (concatenate matrix) operator: defines how image must be placed
            page.Contents.Add(new ConcatenateMatrix(matrix));
            var ximage = page.Resources.Images[page.Resources.Images.Count];
            // using Do operator: this operator draws image
            page.Contents.Add(new Do(ximage.Name));
            // using GRestore operator: this operator restores graphics state
            page.Contents.Add(new GRestore());
            page.FreeMemory();
            Log.Information("Added page for {filePath} to pdf document.", filePath);
        }


        private ImageInfo GetImageInfoEx(string filePath)
        {
            var sw = new Stopwatch();
            sw.Start();
            var jpegFile = imageHelper.ConvertToJpeg(filePath, settings.GroesseInProzent, settings.JpegQualitaetInProzent);
            var size = GetOriginalSizeEx(filePath);
            var bytes = File.ReadAllBytes(jpegFile);
            var stream = new MemoryStream(bytes); // MSDN: disposing is not necessary

            Log.Verbose($"Took {sw.ElapsedMilliseconds} ms to create jpg from jp2 {filePath}", filePath);
            
            // delete the temp file
            if (File.Exists(jpegFile))
            {
                File.Delete(jpegFile);
            }

            return new ImageInfo
            {
                Stream = stream,
                PageSize = size
            };
        }

        private Size GetOriginalSizeEx(string filePath)
        {
            var dpi = imageHelper.GetResolution(filePath);
            var size = imageHelper.GetImageSize(filePath);
            return new Size(72 * size.Width / dpi, 72 * size.Height / dpi);
        }

        private static DateiDIP GetDatei(string dateiRef, List<OrdnerDIP> ordnerList)
        {
            foreach (var ordner in ordnerList)
            {
                foreach (var datei in ordner.Datei)
                {
                    if (datei.Id == dateiRef)
                    {
                        return datei;
                    }
                }

                var dateiSub = GetDatei(dateiRef, ordner.Ordner);
                if (dateiSub != null)
                {
                    return dateiSub;
                }
            }

            return null;
        }

        private static string GetPath(string dateiRef, List<OrdnerDIP> ordnerList, string basePath)
        {
            foreach (var ordner in ordnerList)
            {
                var path = Path.Combine(basePath, ordner.Name);

                foreach (var datei in ordner.Datei)
                {
                    if (datei.Id == dateiRef)
                    {
                        return path;
                    }
                }

                var subPath = GetPath(dateiRef, ordner.Ordner, path);

                if (!string.IsNullOrEmpty(subPath))
                {
                    return subPath;
                }
            }

            return string.Empty;
        }

        private void DeletePremisFiles(List<DateiDIP> premisFiles)
        {
            foreach (var file in premisFiles)
            {
                var path = GetPath(file.Id, paketToConvert.Inhaltsverzeichnis.Ordner, rootFolder);
                var fileName = Path.Combine(path, file.Name);
                if (File.Exists(fileName))
                {
                    MetadataXmlUpdater.RemoveFile(new FileInfo(fileName), paketToConvert, rootFolder);
                    File.Delete(fileName);
                }
            }
        }
    }

    public class ImageInfo
    {
        public Stream Stream { get; set; }
        public Size PageSize { get; set; }
    }
}