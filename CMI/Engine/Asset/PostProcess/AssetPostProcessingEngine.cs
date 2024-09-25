using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Common.Gebrauchskopie;
using Serilog;

namespace CMI.Engine.Asset.PostProcess
{
    public class AssetPostProcessingEngine : IAssetPostProcessingEngine
    {
        private readonly PostProcessCombineTextDocuments textDocumentsProcessor;
        private readonly PostProcessJp2Converter jp2Processor;
        private readonly PostProcessIiifOcrIndexer iiifOcrIndexer;
        private readonly IPostProcessManifestCreator iiifManifestCreator;
        private readonly PostProcessIiifFileDistributor iiifFileDistributor;
        private readonly PostProcessValidIiifFileTypeChecker validIiifFileTypeChecker;

        public AssetPostProcessingEngine(PostProcessCombineTextDocuments textDocumentsProcessor, PostProcessJp2Converter jp2Processor, 
            PostProcessIiifOcrIndexer iiifOcrIndexer, IPostProcessManifestCreator iiifManifestCreator
            , PostProcessIiifFileDistributor iiifFileDistributor, PostProcessValidIiifFileTypeChecker validIiifFileTypeChecker) 
        {
            this.textDocumentsProcessor = textDocumentsProcessor;
            this.jp2Processor = jp2Processor;
            this.iiifOcrIndexer = iiifOcrIndexer;
            this.iiifManifestCreator = iiifManifestCreator;
            this.iiifFileDistributor = iiifFileDistributor;
            this.validIiifFileTypeChecker = validIiifFileTypeChecker;
        }


        public Task<ProcessStepResult> ConvertJp2ToJpeg(string path, ArchiveRecord archiveRecord)
        {
            Log.Information("Start conversion of jp2 files for archiveRecordId {archiveRecordId}.", archiveRecord.ArchiveRecordId);
            var packages = archiveRecord.PrimaryData;
            foreach (var repositoryPackage in packages.Where(p => !string.IsNullOrEmpty(p.PackageFileName)))
            {
                var packageFileName = Path.Combine(path, repositoryPackage.PackageFileName);
                var fi = new FileInfo(packageFileName);

                var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(),
                    fi.Name.Remove(fi.Name.Length - fi.Extension.Length));

                if (Directory.Exists(tempFolder))
                {
                    try
                    {
                        var folder = Path.Combine(tempFolder, "content");
                        jp2Processor.AnalyzeRepositoryPackage(repositoryPackage, folder);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unexpected error while convert jp2 to jpeg. Error Message is: {Message}", ex.Message);
                        return Task.FromResult(new ProcessStepResult
                        {
                            ErrorMessage = $"Unexpected error while convert jp2 to jpeg. Error Message is: {ex.Message}",
                            Success = false
                        });
                    }
                }
                else
                {
                    Log.Warning("Unable to find the unzipped files for {packageFileName}. No jp2 files were converted.", packageFileName);
                    return Task.FromResult(new ProcessStepResult
                    {
                        ErrorMessage = $"Unable to find the unzipped files for {packageFileName}. No jp2 files were converted.",
                        Success = false
                    });
                }
            }

            return Task.FromResult(new ProcessStepResult
            {
                Success = true
            });
        }

        public Task<ProcessStepResult> CombineSinglePageTextExtractsToTextDocument(string path, ArchiveRecord archiveRecord)
        {
            Log.Information("Start combine texts in single Page.");

            var packages = archiveRecord.PrimaryData;
            foreach (var repositoryPackage in packages.Where(p => !string.IsNullOrEmpty(p.PackageFileName)))
            {
                var packageFileName = Path.Combine(path, repositoryPackage.PackageFileName);
                var fi = new FileInfo(packageFileName);

                var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(),
                    fi.Name.Remove(fi.Name.Length - fi.Extension.Length));

                if (Directory.Exists(tempFolder))
                {
                    try
                    {
                        var folder = Path.Combine(tempFolder, "content");
                        textDocumentsProcessor.RootFolder = folder;
                        textDocumentsProcessor.AnalyzeRepositoryPackage(repositoryPackage, folder);
                        textDocumentsProcessor.ZipTextFiles();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unexpected error while combining text documents. Error Message is: {Message}", ex.Message);
                        return Task.FromResult(new ProcessStepResult()
                        {
                            ErrorMessage = $"Unexpected error while combining text documents. Error Message is: {ex.Message}",
                            Success = false
                        });
                    }
                }
                else
                {
                    Log.Warning("Unable to find the unzipped files for {packageFileName}. No text files were combined.", packageFileName);
                    return Task.FromResult(new ProcessStepResult()
                    {
                        ErrorMessage = $"Unable to find the unzipped files for {packageFileName}. No text files were combined.",
                        Success = false
                    });
                }
            }

            return Task.FromResult(new ProcessStepResult
            {
                Success = true
            });
        }

        public Task<ProcessStepResult> SaveOCRTextInSolr(string path, ArchiveRecord archiveRecord)
        {
            Log.Information("Start indexing OCR texts in Solr.");

            var packages = archiveRecord.PrimaryData;
            foreach (var repositoryPackage in packages.Where(p => !string.IsNullOrEmpty(p.PackageFileName)))
            {
                var packageFileName = Path.Combine(path, repositoryPackage.PackageFileName);
                var fi = new FileInfo(packageFileName);
               
                var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(),
                    fi.Name.Remove(fi.Name.Length - fi.Extension.Length));


                if (Directory.Exists(tempFolder))
                {
                    try
                    {
                        var folder = Path.Combine(tempFolder, "content");
                        var metadataFile = new FileInfo(Path.Combine(tempFolder, "header", "metadata.xml"));

                        iiifOcrIndexer.RootFolder = folder;
                        iiifOcrIndexer.Paket = (PaketDIP) Paket.LoadFromFile(metadataFile.FullName);
                        iiifOcrIndexer.ArchiveRecordId = repositoryPackage.ArchiveRecordId;
                        iiifOcrIndexer.AnalyzeRepositoryPackage(repositoryPackage, folder);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unexpected error while indexing ocr documents. Error Message is: {Message}", ex.Message);
                        return Task.FromResult(new ProcessStepResult
                        {
                            ErrorMessage = $"Unexpected error while indexing ocr documents. Error Message is: {ex.Message}",
                            Success = false
                        });
                    }
                }
                else
                {
                    Log.Warning("Unable to find the unzipped files for {packageFileName}. No text files were indexed in solr.", packageFileName);
                    return Task.FromResult(new ProcessStepResult
                    {
                        ErrorMessage = $"Unable to find the unzipped files for {packageFileName}. No text files were indexed in solr.",
                        Success = false
                    });
                }
            }
            Log.Information("Finish indexing OCR texts in Solr.");
            
            return Task.FromResult(new ProcessStepResult
            {
                Success = true
            });
        }

        public Task<ProcessStepResult> CreateIiifManifests(string path, ArchiveRecord archiveRecord)
        {
            Log.Information("Start creation of iiif manifest files for archiveRecordId {archiveRecordId}.", archiveRecord.ArchiveRecordId);
            var packages = archiveRecord.PrimaryData;
            foreach (var repositoryPackage in packages.Where(p => !string.IsNullOrEmpty(p.PackageFileName)))
            {
                var packageFileName = Path.Combine(path, repositoryPackage.PackageFileName);
                var fi = new FileInfo(packageFileName);

                var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(),
                    fi.Name.Remove(fi.Name.Length - fi.Extension.Length));

                if (Directory.Exists(tempFolder))
                {
                    try
                    {
                        var metadataFile = new FileInfo(Path.Combine(tempFolder, "header", "metadata.xml"));
                        var paket = (PaketDIP) Paket.LoadFromFile(metadataFile.FullName);
                        var manifestLink = iiifManifestCreator.CreateManifest(archiveRecord.ArchiveRecordId, paket, tempFolder);
                        archiveRecord.Metadata.ManifestLink = manifestLink.ToString();
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unexpected error while creating iiif manifests. Error Message is: {Message}", ex.Message);
                        return Task.FromResult(new ProcessStepResult
                        {
                            ErrorMessage = $"Unexpected error while creating iiif manifests. Error Message is: {ex.Message}",
                            Success = false
                        });
                    }
                }
                else
                {
                    Log.Warning("Unable to find the unzipped files for {packageFileName}. No iiif manifests were created.", packageFileName);
                    return Task.FromResult(new ProcessStepResult
                    {
                        ErrorMessage = $"Unable to find the unzipped files for {packageFileName}. No iiif manifests were created.",
                        Success = false
                    });
                }
            }

            return Task.FromResult(new ProcessStepResult
            {
                Success = true
            });
        }

        public Task<ProcessStepResult> DistributeIiifFiles(string path, ArchiveRecord archiveRecord)
        {
            Log.Information("Start distribution of iiif files for archiveRecordId {archiveRecordId}.", archiveRecord.ArchiveRecordId);
            var packages = archiveRecord.PrimaryData;
            foreach (var repositoryPackage in packages.Where(p => !string.IsNullOrEmpty(p.PackageFileName)))
            {
                var packageFileName = Path.Combine(path, repositoryPackage.PackageFileName);
                var fi = new FileInfo(packageFileName);

                var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(),
                    fi.Name.Remove(fi.Name.Length - fi.Extension.Length));

                if (Directory.Exists(tempFolder))
                {
                    try
                    {
                        var folder = Path.Combine(tempFolder, "content");
                        iiifFileDistributor.RootFolder = folder;
                        iiifFileDistributor.ArchiveRecordId = repositoryPackage.ArchiveRecordId;
                        iiifFileDistributor.AnalyzeRepositoryPackage(repositoryPackage, folder);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unexpected error while distributing iiif files. Error Message is: {Message}", ex.Message);
                        return Task.FromResult(new ProcessStepResult
                        {
                            ErrorMessage = $"Unexpected error while distributing iiif files. Error Message is: {ex.Message}",
                            Success = false
                        });
                    }
                }
                else
                {
                    Log.Warning("Unable to find the unzipped files for {packageFileName}. No iiif files were distributed.", packageFileName);
                    return Task.FromResult(new ProcessStepResult
                    {
                        ErrorMessage = $"Unable to find the unzipped files for {packageFileName}. No iiif files were distributed.",
                        Success = false
                    });
                }
            }

            return Task.FromResult(new ProcessStepResult
            {
                Success = true
            });
        }

        public Task<ProcessStepResult> ContainsOnlyValidFileTypes(string path, ArchiveRecord archiveRecord)
        {
            Log.Information("Start checking for valid IIIF file types for archiveRecordId {archiveRecordId}",
                archiveRecord.ArchiveRecordId);
            var packages = archiveRecord.PrimaryData;
            foreach (var repositoryPackage in packages.Where(p => !string.IsNullOrEmpty(p.PackageFileName)))
            {
                var packageFileName = Path.Combine(path, repositoryPackage.PackageFileName);
                var fi = new FileInfo(packageFileName);

                var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(),
                    fi.Name.Remove(fi.Name.Length - fi.Extension.Length));

                if (Directory.Exists(tempFolder))
                {
                    try
                    {
                        var folder = Path.Combine(tempFolder, "content");
                        validIiifFileTypeChecker.ArchiveRecordId = repositoryPackage.ArchiveRecordId;
                        validIiifFileTypeChecker.AnalyzeRepositoryPackage(repositoryPackage, folder);
                    }
                    catch (Exception ex)
                    {
                        Log.Information(ex, "Error while checking valid IIIF file types. Probably invalid file type. Message is: {Message}", ex.Message);
                        return Task.FromResult(new ProcessStepResult
                        {
                            ErrorMessage = $"Error while checking valid IIIF file types. Probably invalid file type. Message is: {ex.Message}",
                            Success = false
                        });
                    }
                }
                else
                {
                    Log.Warning("Unable to find the unzipped files for {packageFileName}. No iiif files were distributed.", packageFileName);
                    return Task.FromResult(new ProcessStepResult
                    {
                        ErrorMessage = $"Unable to find the unzipped files for {packageFileName}. No iiif files were distributed.",
                        Success = false
                    });
                }
            }

            return Task.FromResult(new ProcessStepResult
            {
                Success = true
            });
        }
    }

}
