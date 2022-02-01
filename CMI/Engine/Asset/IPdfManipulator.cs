using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Aspose.Pdf;
using Aspose.Pdf.Text;
using CMI.Contract.Common;
using Serilog;
using License = Aspose.Pdf.License;

namespace CMI.Engine.Asset
{
    public interface IPdfManipulator
    {
        /// <summary>
        /// Converts a list of repository files into a list of text extraction files that are later used to
        /// start the text extraction of those file.
        /// This methods checks each file.
        /// 
        ///   - PDF File:   if a pdf file contains more than 200 pages, the method will split the file on disk into smaller
        ///                 pdf files with a prefix like 0001_
        ///                 The method will add the additional files as <see cref="AssetExtractionFile"/> to the returned list.
        ///                 The added files will contain a reference to the original file.
        ///   - All other:  All other file types will simply be converted to <see cref="AssetExtractionFile"/>.
        /// </summary>
        /// <param name="repositoryFiles">A list with <see cref="RepositoryFile"/></param>
        /// <param name="path">The base path where the passed files are stored on disk</param>
        /// <returns>A list with <see cref="AssetExtractionFile"/> that also contains the splitted files if any.</returns>
        List<AssetExtractionFile> ConvertToTextExtractionFiles(List<RepositoryFile> repositoryFiles, string path);

        /// <summary>
        /// Converts a list of FileInfo objects into a list of <see cref="AssetConversionFile"/> that are later used
        /// to convert those files.
        /// This methods checks each file.
        /// 
        ///   - PDF File:   if a pdf file contains more than 200 pages and does not contain a text layer,
        ///                 the method will split the file on disk into smaller pdf files with a prefix like 0001_
        ///                 The method will add the additional files as <see cref="AssetExtractionFile"/> to the returned list.
        ///                 The added files will contain a reference to the original file.
        ///   - All other:  All other file types will simply be converted to <see cref="AssetExtractionFile"/>.
        /// </summary>
        /// <param name="repositoryFiles">The list of files to convert</param>
        /// <param name="tempFolder">The folder in which the repository files can be found</param>
        /// <param name="skipIfContainsTextLayer">if set to true, pdf with a textlayer are not splitted</param>
        /// <returns></returns>
        List<AssetConversionFile> ConvertToConversionFiles(List<RepositoryFile> repositoryFiles, string tempFolder, bool skipIfContainsTextLayer);
        
        /// <summary>
        /// Will split the file in smaller parts of roughly 100 pages.
        /// If a document with less than 100 pages is feed to the method, one splitted file is returned
        /// </summary>
        /// <param name="pdfFile">The file to split</param>
        /// <param name="fileId">The id of the original file</param>
        /// <returns>A list with the newly created splitted files</returns>
        List<T> SplitPdfFile<T>(FileInfo pdfFile, string fileId) where T : AssetFileBase, new();

        /// <summary>
        /// Loops through each <see cref="RepositoryFile"/> and transfers the extracted text found in the
        /// corresponding <see cref="AssetExtractionFile"/> to the repository file.
        /// If a file was splitted, all extracted text from the splitted parts are transferred in the correct order
        /// </summary>
        /// <param name="textExtractionFiles"></param>
        /// <param name="repositoryFiles"></param>
        void TransferExtractedText(List<AssetExtractionFile> textExtractionFiles, List<RepositoryFile> repositoryFiles);

        /// <summary>
        /// Indicates if the file needs to be splitted. Applies only to PDF files
        /// </summary>
        /// <param name="pdfFile">The file to check</param>
        /// <param name="skipIfContainsTextLayer">If set to true, the method will return false if the
        /// pdf file has more than the allowed number of pages, if it contains a text layer.</param>
        /// <returns></returns>
        bool NeedsSplitting(FileInfo pdfFile, bool skipIfContainsTextLayer);

        /// <summary>
        /// Merges back splitted files according to the metadata information in the passed list
        /// </summary>
        /// <param name="conversionFiles">The list with converted files</param>
        void MergeSplittedFiles(List<AssetConversionFile> conversionFiles);
    }

    public class PdfManipulator : IPdfManipulator
    {
        private const int splitThreshold = 200;

        public PdfManipulator()
        {
            var licensePdf = new License();
            licensePdf.SetLicense("Aspose.Total.lic");
        }

        public List<AssetExtractionFile> ConvertToTextExtractionFiles(List<RepositoryFile> repositoryFiles, string path)
        {
            var retVal = new List<AssetExtractionFile>();

            try
            {
                foreach (var repositoryFile in repositoryFiles)
                {
                    var isSplitted = false;

                    // Skip files that were not exported and skip files that should be skipped
                    if (!repositoryFile.Exported || repositoryFile.SkipOCR)
                    {
                        Log.Information("Skipping {repositoryFile} as it was not downloaded from the repository or should be skipped", repositoryFile.PhysicalName);
                        continue;
                    }

                    var diskFile = new FileInfo(Path.Combine(path, repositoryFile.PhysicalName));
                    if (diskFile.Extension.Equals(".pdf", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Split if necessary
                        if (NeedsSplitting(diskFile, true))
                        {
                            isSplitted = true;
                            retVal.AddRange(SplitPdfFile<AssetExtractionFile>(diskFile, repositoryFile.Id));
                        }
                    }

                    // add info about the original if it was not splitted
                    if (!isSplitted)
                    {
                        retVal.Add(new AssetExtractionFile
                        {
                            Id = repositoryFile.Id,
                            FullName = diskFile.FullName
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while converting repository files to text extraction files.");
                throw;
            }

            return retVal;
        }

        public List<AssetConversionFile> ConvertToConversionFiles(List<RepositoryFile> repositoryFiles, string tempFolder, bool skipIfContainsTextLayer)
        {
            var retVal = new List<AssetConversionFile>();

            try
            {
                foreach (var repositoryFile in repositoryFiles)
                {
                    var isSplitted = false;

                    // Skip files that were not exported and skip files that should be skipped
                    if (!repositoryFile.Exported || repositoryFile.SkipOCR)
                    {
                        Log.Information("Skipping {repositoryFile} as it was not downloaded from the repository or should be skipped", repositoryFile.PhysicalName);
                        continue;
                    }

                    var file = new FileInfo(Path.Combine(tempFolder, repositoryFile.PhysicalName));

                    if (file.Extension.Equals(".pdf", StringComparison.InvariantCultureIgnoreCase))
                    {
                        // Split if necessary
                        if (NeedsSplitting(file, skipIfContainsTextLayer))
                        {
                            isSplitted = true;
                            retVal.AddRange(SplitPdfFile<AssetConversionFile>(file, file.FullName));
                        }
                    }

                    // add info about the original file if it was not splitted
                    if (!isSplitted)
                    {
                        retVal.Add(new AssetConversionFile
                        {
                            Id = file.FullName,
                            FullName = file.FullName
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while converting files to conversion files.");
                throw;
            }

            return retVal;
        }


        public List<T> SplitPdfFile<T>(FileInfo pdfFile, string fileId) where T : AssetFileBase, new()
        {
            var retVal = new List<T>();

            // Check if pdf file
            if (!pdfFile.Extension.Equals(".pdf", StringComparison.InvariantCultureIgnoreCase))
            {
                throw new InvalidOperationException($"You must pass a pdf file to split. {pdfFile.FullName} is not a pdf file.");
            }

            using (var pdfDocument = new Document(pdfFile.FullName))
            {
                var totalPages = pdfDocument.Pages.Count;
                var documentSize = (int) Math.Ceiling(totalPages / Math.Ceiling(totalPages / 100d));

                var partNumber = 1;
                var from = 1;

                do
                {
                    var newDocument = new Document();
                    for (int i = 0; i < documentSize; i++)
                    {
                        // Add the page from the original pdf to the new one
                        if (pdfDocument.Pages.Count >= from + i)
                        {
                            newDocument.Pages.Add(pdfDocument.Pages[from + i]);
                        }
                    }

                    var newPhysicalName = $"{partNumber:d4}_" + pdfFile.Name;
                    var newFileName = Path.Combine(pdfFile.DirectoryName ?? "", newPhysicalName);
                    newDocument.Save(newFileName);

                    var newFile = new T
                    {
                        FullName = newFileName,
                        ParentId = fileId,
                        SplitPartNumber = partNumber
                    };
                    retVal.Add(newFile);

                    from += documentSize;
                    partNumber++;

                } while (from < totalPages);
            }

            return retVal;
        }

        public void TransferExtractedText(List<AssetExtractionFile> textExtractionFiles, List<RepositoryFile> repositoryFiles)
        {
            foreach (var repositoryFile in repositoryFiles)
            {
                // Only exported files can contain extracted text
                if (repositoryFile.Exported)
                {
                    var parts = textExtractionFiles.Where(t => t.ParentId == repositoryFile.Id)
                        .OrderBy(t => t.SplitPartNumber).ToList();
                    if (parts.Any())
                    {
                        var sb = new StringBuilder();
                        foreach (var textExtractionFile in parts)
                        {
                            sb.AppendLine(textExtractionFile.ContentText);
                        }
                        // Transfer all text
                        repositoryFile.ContentText = sb.ToString();
                    }
                    else
                    {
                        // Find the text extraction File
                        var textFile = textExtractionFiles.FirstOrDefault(t => !string.IsNullOrEmpty(t.Id) && t.Id == repositoryFile.Id);
                        if (textFile != null)
                        {
                            repositoryFile.ContentText = textFile.ContentText;
                        }
                    }
                }
            }
        }

        public bool NeedsSplitting(FileInfo pdfFile, bool skipIfContainsTextLayer)
        {
            if (pdfFile.Extension.Equals(".pdf", StringComparison.InvariantCultureIgnoreCase))
            {
                using (var pdfDocument = new Document(pdfFile.FullName))
                {
                    if (!skipIfContainsTextLayer || !HasText(pdfDocument))
                    {
                        return pdfDocument.Pages.Count > splitThreshold;
                    }
                }
            }

            return false;
        }

        public void MergeSplittedFiles(List<AssetConversionFile> conversionFiles)
        {
            // Get the list with the files that need to be merged
            var filesToMerge = conversionFiles.Where(f => !string.IsNullOrEmpty(f.ParentId)).OrderBy(f => f.FullName).ToList();

            // The parent id contains the original filename, so we get those
            var originalFileNames = filesToMerge.Select(f => f.ParentId).Distinct();

            // Now delete these files from the original list, as we will add the info about the merged file
            filesToMerge.ForEach(mergeFile =>
            {
                var toDelete = conversionFiles.FirstOrDefault(f => f.FullName == mergeFile.FullName);
                conversionFiles.Remove(toDelete);
            });

            foreach (var originalFileName in originalFileNames)
            {
                // Get the new resulting document
                using (var pdfDocument = GetMetadataClone(originalFileName))
                {

                    // Add all the pages from the splitted parts
                    var parts = filesToMerge.Where(f => f.ParentId == originalFileName).OrderBy(f => f.SplitPartNumber).ToList();
                    var partList = new List<Document>();
                    foreach (var part in parts)
                    {
                        var pdfPart = new Document(part.FullName);
                        partList.Add(pdfPart);
                        pdfDocument.Pages.Add(pdfPart.Pages);
                    }

                    // Delete the old original
                    File.Delete(originalFileName);

                    // Save the newly created pdf
                   var optimizationOptions = new Document.OptimizationOptions
                   {
                       RemoveUnusedObjects = true,
                       RemoveUnusedStreams = true,
                       AllowReusePageContent = true,
                       LinkDuplcateStreams = true,
                       UnembedFonts = false
                   };

                    pdfDocument.OptimizeResources(optimizationOptions);
                    pdfDocument.Optimize();
                    pdfDocument.Save(originalFileName);

                    // Clean up resources
                    partList.ForEach(document =>
                    {
                        File.Delete(document.FileName);
                        document.Dispose();
                    });

                    // Finally add that file to the list of files
                    conversionFiles.Add(new AssetConversionFile()
                    {
                        FullName = originalFileName,
                        ConvertedFile = originalFileName
                    });
                }
            }
        }

        /// <summary>
        /// Creates a new pdf document that contains the same metadata information as the old one
        /// </summary>
        /// <param name="originalFileName"></param>
        /// <returns></returns>
        private Document GetMetadataClone(string originalFileName)
        {
            var retVal = new Document();
            using (var original = new Document(originalFileName))
            {
                var docInfo = new DocumentInfo(retVal);

                // Copy all properties.
                // exceptions: Creator and Producer are read-only and cannot be set
                docInfo.Author = original.Info.Author;
                docInfo.CreationDate = original.Info.CreationDate;
                docInfo.CreationTimeZone = original.Info.CreationTimeZone;
                docInfo.Keywords = original.Info.Keywords;
                docInfo.ModDate = original.Info.ModDate;
                docInfo.ModTimeZone = original.Info.ModTimeZone;
                docInfo.Subject = original.Info.Subject;
                docInfo.Title = original.Info.Title;
                docInfo.Trapped = original.Info.Trapped;
            }

            return retVal;
        }

        internal static bool HasText(Document document)
        {
            try
            {
                var ta = new TextAbsorber();
                foreach (Page page in document.Pages)
                {
                    page.Accept(ta);

                    if (ta.Text.Trim(' ', '\n', '\r').Length != 0)
                    {
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "HasText für {fileName} failed.", document.FileName);
            }

            return false;
        }
    }
}
