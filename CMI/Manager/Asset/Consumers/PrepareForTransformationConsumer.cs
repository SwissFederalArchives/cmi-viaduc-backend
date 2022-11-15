using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Contract.Messaging;
using CMI.Engine.Asset;
using CMI.Manager.Asset.Properties;
using MassTransit;
using Serilog;
using Serilog.Core.Enrichers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Asset.Consumers
{
    public class PrepareForTransformationConsumer : IConsumer<PrepareForTransformationMessage>
    {
        private readonly IAssetManager assetManager;
        private readonly IScanProcessor scanProcessor;
        private readonly ITransformEngine transformEngine;
        private readonly IAssetPreparationEngine assetPreparationEngine;
        private List<Func<PrepareForTransformationMessage, Task<PreprocessingResult>>> preparationSteps;

        public PrepareForTransformationConsumer(IAssetManager assetManager, IScanProcessor scanProcessor, ITransformEngine transformEngine, IAssetPreparationEngine assetPreparationEngine)
        {
            this.assetManager = assetManager;
            this.scanProcessor = scanProcessor;
            this.transformEngine = transformEngine;
            this.assetPreparationEngine = assetPreparationEngine;
            preparationSteps = new List<Func<PrepareForTransformationMessage, Task<PreprocessingResult>>>();
        }

        public async Task Consume(ConsumeContext<PrepareForTransformationMessage> context)
        {
            var conversationEnricher = new PropertyEnricher(nameof(context.ConversationId), context.ConversationId);
            var archiveRecordIdEnricher = new PropertyEnricher(nameof(context.Message.RepositoryPackage.ArchiveRecordId), context.Message.RepositoryPackage.ArchiveRecordId);
            var packageIdEnricher = new PropertyEnricher(nameof(context.Message.RepositoryPackage.PackageId), context.Message.RepositoryPackage.PackageId);

            using (LogContext.Push(conversationEnricher, archiveRecordIdEnricher, packageIdEnricher))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(PrepareForTransformationMessage), context.ConversationId);

                // 1. Step: Extract Zip file(s)
                preparationSteps.Add(ExtractRepositoryPackage);
                // 2. Step: ConvertAreldaXml
                preparationSteps.Add(ConvertAreldaMetadataXml);
                // 3. Step: ConvertJp2ToPdf
                preparationSteps.Add(ConvertSingleJp2ToPdf);
                // 4. Step: Detect and mark files with large dimensions
                preparationSteps.Add(DetectAndFlagLargeDimensions);
                // 5. Step: Detect and optimize pdf files that need optimization
                preparationSteps.Add(DetectAndOptimizePdf);


                foreach (var step in preparationSteps)
                {
                    var result = await step(context.Message);

                    // In case any step was not successful, fail the sync
                    if (!result.Success)
                    {
                        await PublishAssetReadyFailed(context, result.ErrorMessage);
                        return;
                    }
                }

                // Forward the prepared data to the next processing point
                var endpoint = await context.GetSendEndpoint(new Uri(context.SourceAddress, BusConstants.AssetManagerTransformAssetMessageQueue));

                await endpoint.Send(new TransformAsset
                {
                    AssetType = context.Message.AssetType,
                    OrderItemId = context.Message.OrderItemId,
                    CallerId = context.Message.CallerId,
                    RetentionCategory = context.Message.RetentionCategory,
                    Recipient = context.Message.Recipient,
                    Language = context.Message.Language,
                    ProtectWithPassword = context.Message.ProtectWithPassword,
                    PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                    RepositoryPackage= context.Message.RepositoryPackage
                });
            }
        }

        /// <summary>
        /// In case we have a Benutzungskopie, we need to transform the metadata.xml
        /// </summary>
        /// <param name="message"></param>
        private Task<PreprocessingResult> ConvertAreldaMetadataXml(PrepareForTransformationMessage message)
        {
            try
            {
                var tempFolder = GetTempFolder(message.RepositoryPackage); 

                if (message.AssetType == AssetType.Benutzungskopie)
                {
                    transformEngine.ConvertAreldaMetadataXml(tempFolder);
                }

                return Task.FromResult(new PreprocessingResult {Success = true});
            }
            catch (Exception ex)
            {
                var msg = "Unexpected error while converting to AreldaMetadata xml.";
                Log.Error(ex, msg);
                return Task.FromResult(new PreprocessingResult {Success = false, ErrorMessage = msg});
            }
        }

        private Task<PreprocessingResult> DetectAndFlagLargeDimensions(PrepareForTransformationMessage message)
        {
            try
            {
                // do the detection
                var tempFolder = Path.Combine(GetTempFolder(message.RepositoryPackage), "content");
                assetPreparationEngine.DetectAndFlagLargeDimensions(message.RepositoryPackage, tempFolder, message.PrimaerdatenAuftragId);

                return Task.FromResult(new PreprocessingResult { Success = true });
            }
            catch (Exception ex)
            {
                var msg = "Unexpected error while detecting large dimensions.";
                Log.Error(ex, msg);
                return Task.FromResult(new PreprocessingResult { Success = false, ErrorMessage = msg });
            }
        }

        private Task<PreprocessingResult> DetectAndOptimizePdf(PrepareForTransformationMessage message)
        {
            try
            {
                // Now do the optimization
                var tempFolder = Path.Combine(GetTempFolder(message.RepositoryPackage), "content");  
                assetPreparationEngine.OptimizePdfIfRequired(message.RepositoryPackage, tempFolder, message.PrimaerdatenAuftragId);

                return Task.FromResult(new PreprocessingResult { Success = true });
            }
            catch (Exception ex)
            {
                var msg = "Unexpected error while detecting and optimizing pdf.";
                Log.Error(ex, msg);
                return Task.FromResult(new PreprocessingResult { Success = false, ErrorMessage = msg });
            }
        }

        private Task<PreprocessingResult> ConvertSingleJp2ToPdf(PrepareForTransformationMessage message)
        {
            try
            {
                var tempFolder = GetTempFolder(message.RepositoryPackage);

                var metadataFile = Path.Combine(tempFolder, "header", "metadata.xml");
                var paket = (PaketDIP)Paket.LoadFromFile(metadataFile);

                // Create pdf documents from scanned jpeg 2000 scans.
                scanProcessor.ConvertSingleJpeg2000ScansToPdfDocuments(paket, tempFolder);

                // Save the changed info to the metadata file
                ((Paket)paket).SaveToFile(metadataFile);

                // As we changed files we need to update the RepositoryPackage 
                if (paket.Ablieferung.Bemerkung != "Metadata.xml das nicht zum Inhalt passt für Testsysteme")
                {
                    UpdateRepositoryPackage(message.RepositoryPackage, paket);
                }
                else
                {
                    UpdateRepositoryPackageFromDisk(message.RepositoryPackage, tempFolder);
                }


                return Task.FromResult(new PreprocessingResult { Success = true });
            }
            catch (Exception ex)
            {
                var msg = "Unexpected error while converting single jpeg 2000 to pdf.";
                Log.Error(ex, msg);
                return Task.FromResult(new PreprocessingResult { Success = false, ErrorMessage = msg });
            }
        }

        /// This version adds all the files from the file system to the repro package
        /// Only required for development, as normally the package is updated from the metadata.xml
        private void UpdateRepositoryPackageFromDisk(RepositoryPackage repositoryPackage, string tempFolder)
        {
            repositoryPackage.Files.Clear();
            repositoryPackage.Folders.Clear();

            var contentFolder = Path.Combine(tempFolder, "content");
            contentFolder = contentFolder.EndsWith("\\") ? contentFolder : contentFolder + "\\";
            var files = new DirectoryInfo(contentFolder).GetFiles("*.*", SearchOption.AllDirectories);
            repositoryPackage.Files.AddRange(files.Select(f => new RepositoryFile
            {
                Id = f.FullName,
                PhysicalName = f.FullName.Replace(contentFolder, ""),
                Exported = true
            }));
        }

        /// <summary>
        /// This procedure updates the repository package so it correctly reflects the contents of the package
        /// </summary>
        /// <param name="repositoryPackage"></param>
        /// <param name="paket"></param>
        private void UpdateRepositoryPackage(RepositoryPackage repositoryPackage, PaketDIP paket)
        {
            repositoryPackage.Files.Clear();
            repositoryPackage.Folders.Clear();

            var contentFolder = paket.Inhaltsverzeichnis.Ordner;
            Debug.Assert(contentFolder.Count == 1, "There should be only one folder at the content level");
            repositoryPackage.Files.AddRange(ConvertToRepositoryFiles(contentFolder.First().Datei));
            repositoryPackage.Folders.AddRange(ConvertToRepositoryFolders(contentFolder.First().Ordner));
        }

        private List<RepositoryFolder> ConvertToRepositoryFolders(List<OrdnerDIP> folders)
        {
            var list = new List<RepositoryFolder>();
            foreach (var folder in folders)
            {
                var newFolder = new RepositoryFolder
                {
                    Id = folder.Id,
                    PhysicalName = folder.Name,
                    LogicalName = folder.OriginalName,
                };
                newFolder.Files.AddRange(ConvertToRepositoryFiles(folder.Datei));
                newFolder.Folders.AddRange(ConvertToRepositoryFolders(folder.Ordner));
                list.Add(newFolder);
            }

            return list;
        }

        private List<RepositoryFile> ConvertToRepositoryFiles(IList<DateiDIP> files)
        {
            var list = new List<RepositoryFile>();
            foreach (var file in files)
            {
                list.Add(new RepositoryFile
                {
                    Id = file.Id,
                    PhysicalName = file.Name,
                    Exported = true,
                    SipOriginalName = file.OriginalName,
                    Hash = file.Pruefsumme,
                    HashAlgorithm = file.Pruefalgorithmus.ToString()
                });
            }

            return list;
        }

        private string GetTempFolder(RepositoryPackage package)
        {
            var packageFileName = Path.Combine(Settings.Default.PickupPath, package.PackageFileName);
            var fi = new FileInfo(packageFileName);
            var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(), fi.Name.Remove(fi.Name.Length - fi.Extension.Length));
            return tempFolder;
        }

        private async Task PublishAssetReadyFailed(ConsumeContext<PrepareForTransformationMessage> context, string errorMessage)
        {
            try
            {
                var message = context.Message;
                // If we do have an error, we stop the sync process here.
                Log.Error(
                    "Failed to preprocess the package for transformation for archiveRecord {archiveRecordId} with conversationId {ConversationId}",
                    message.RepositoryPackage.ArchiveRecordId, context.ConversationId);

                var assetReady = new AssetReady
                {
                    ArchiveRecordId = message.RepositoryPackage.ArchiveRecordId,
                    OrderItemId = message.OrderItemId,
                    CallerId = message.CallerId,
                    AssetType = message.AssetType,
                    Recipient = message.Recipient,
                    RetentionCategory = message.RetentionCategory,
                    PrimaerdatenAuftragId = message.PrimaerdatenAuftragId,
                    // We have failed in the conversion inform the world about the failed package
                    Valid = false,
                    ErrorMessage = errorMessage
                };

                await assetManager.UpdatePrimaerdatenAuftragStatus(new UpdatePrimaerdatenAuftragStatus
                {
                    PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                    Service = AufbereitungsServices.AssetService,
                    Status = AufbereitungsStatusEnum.PreprocessingAbgeschlossen,
                    ErrorText = errorMessage
                });

                // inform the world about the failed package
                await context.Publish<IAssetReady>(assetReady);

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while publish a failed AssetReady event.");
            }
            finally
            {
                // Delete the temp files
                var tempFolder = GetTempFolder(context.Message.RepositoryPackage);
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                }
            }
        }

        private async Task<PreprocessingResult> ExtractRepositoryPackage(PrepareForTransformationMessage message)
        {
            var packageFileName = message.RepositoryPackage.PackageFileName;
            var primaerdatenAuftragId = message.PrimaerdatenAuftragId;

            var success = await assetManager.ExtractZipFile(new ExtractZipArgument
            {
                PackageFileName = packageFileName,
                PrimaerdatenAuftragId = primaerdatenAuftragId
            });

            return success ? new PreprocessingResult {Success = true} : 
                             new PreprocessingResult {Success = false, ErrorMessage = $"Failed to unzip package {packageFileName}"};
        }
    }
}