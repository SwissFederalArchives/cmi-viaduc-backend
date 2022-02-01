using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Engine.Asset;
using CMI.Manager.Asset.Properties;
using MassTransit;
using Serilog;
using Serilog.Context;
using Serilog.Core.Enrichers;

namespace CMI.Manager.Asset.Consumers
{
    /// <summary>
    /// This consumer allows a preparation / modification of the primary data that is going to be
    /// OCR recognized in the following step.
    /// Thus we can use this preprocessing to analyze the documents and detect problem cases like:
    /// 1. PDF files that are not optimized and are huge in size, because they contain uncompressed tiff files
    /// 2. PDF files that contain large pages larger than A3 or bigger. This usually indicates maps or posters that
    ///    are not interesting for OCR anyway
    ///
    /// To skip files from OCR add an extended property to the <see cref="RepositoryFile"/> to indicate that the file
    /// should be skipped.
    /// </summary>
    public class PrepareForRecognitionConsumer : IConsumer<PrepareForRecognitionMessage>
    {
        private readonly IAssetManager assetManager;
        private readonly IAssetPreparationEngine assetPreparationEngine;
        private List<Func<PrepareForRecognitionMessage, Task<PreprocessingResult>>> preparationSteps;

        public PrepareForRecognitionConsumer(IAssetManager assetManager, IAssetPreparationEngine assetPreparationEngine)
        {
            this.assetManager = assetManager;
            this.assetPreparationEngine = assetPreparationEngine;
            preparationSteps = new List<Func<PrepareForRecognitionMessage, Task<PreprocessingResult>>>();
        }

        public async Task Consume(ConsumeContext<PrepareForRecognitionMessage> context)
        {
            var conversationEnricher = new PropertyEnricher(nameof(context.ConversationId), context.ConversationId);
            var mutationIdEnricher = new PropertyEnricher(nameof(context.Message.MutationId), context.Message.MutationId);
            var primaerdatenAuftragIdEnricher = new PropertyEnricher(nameof(context.Message.PrimaerdatenAuftragId), context.Message.PrimaerdatenAuftragId);

            using (LogContext.Push(conversationEnricher, mutationIdEnricher, primaerdatenAuftragIdEnricher))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(PrepareForRecognitionMessage), context.ConversationId);


                // 1. Step: Extract Zip file(s)
                preparationSteps.Add(ExtractRepositoryPackage);
                // 2. Step: Detect and mark files with large dimensions
                preparationSteps.Add(DetectAndFlagLargeDimensions);
                // 3. Step: Detect and optimize pdf files that need optimization
                preparationSteps.Add(DetectAndOptimizePdf);

                foreach (var step in preparationSteps)
                {
                    var result = await step(context.Message);

                    // In case any step was not successful, fail the sync
                    if (!result.Success)
                    {
                        await SendFailSync(context, result.ErrorMessage);
                        return;
                    }
                }

                await assetManager.UpdatePrimaerdatenAuftragStatus(new UpdatePrimaerdatenAuftragStatus
                {
                    PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                    Service = AufbereitungsServices.AssetService,
                    Status = AufbereitungsStatusEnum.PreprocessingAbgeschlossen
                });

                // Forward the prepared data to the next processing point
                var endpoint = await context.GetSendEndpoint(new Uri(context.SourceAddress, BusConstants.AssetManagerExtractFulltextMessageQueue));

                await endpoint.Send<IArchiveRecordExtractFulltextFromPackage>(new ArchiveRecordExtractFulltextFromPackage
                {
                    MutationId = context.Message.MutationId,
                    ArchiveRecord = context.Message.ArchiveRecord,
                    PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId
                });
            }
        }

        private async Task<PreprocessingResult> ExtractRepositoryPackage(PrepareForRecognitionMessage message)
        {
            var packages = message.ArchiveRecord.PrimaryData;
            var primaerdatenAuftragId = message.PrimaerdatenAuftragId;

            var repositoryPackage = packages.FirstOrDefault(p => !string.IsNullOrEmpty(p.PackageFileName));
            if (repositoryPackage != null)
            {
                var result = await assetManager.ExtractZipFile(new ExtractZipArgument
                {
                    PackageFileName = repositoryPackage.PackageFileName, 
                    PrimaerdatenAuftragId = primaerdatenAuftragId
                });
                return result ? new PreprocessingResult{Success = true} : 
                                new PreprocessingResult{Success = false, ErrorMessage = "Could not unzip package."};
            }

            Log.Warning("No package found for PrimaerdatenAuftrag with id {primaerdatenAuftragId} where one was expected.", primaerdatenAuftragId);
            return new PreprocessingResult{Success = false, ErrorMessage = $"No package found for PrimaerdatenAuftrag with id {primaerdatenAuftragId} where one was expected." };
        }

        private Task<PreprocessingResult> DetectAndFlagLargeDimensions(PrepareForRecognitionMessage message)
        {
            try
            {
                foreach (var package in message.ArchiveRecord.PrimaryData)
                {
                    var tempFolder = Path.Combine(GetTempFolder(package), "content");

                    // Now do the detection
                    assetPreparationEngine.DetectAndFlagLargeDimensions(package, tempFolder, message.PrimaerdatenAuftragId);
                }

                return Task.FromResult(new PreprocessingResult { Success = true });
            }
            catch (Exception ex)
            {
                var msg = "Unexpected error while detecting large dimensions.";
                Log.Error(ex, msg);
                return Task.FromResult(new PreprocessingResult { Success = false, ErrorMessage = msg });
            }
        }

        private Task<PreprocessingResult> DetectAndOptimizePdf(PrepareForRecognitionMessage message)
        {
            try
            {

                foreach (var package in message.ArchiveRecord.PrimaryData)
                {
                    var tempFolder = Path.Combine(GetTempFolder(package), "content");
                    // Now do the detection
                    assetPreparationEngine.OptimizePdfIfRequired(package, tempFolder, message.PrimaerdatenAuftragId);
                }

                return Task.FromResult(new PreprocessingResult { Success = true });
            }
            catch (Exception ex)
            {
                var msg = "Unexpected error while detecting and optimizing pdf.";
                Log.Error(ex, msg);
                return Task.FromResult(new PreprocessingResult { Success = false, ErrorMessage = msg });
            }
        }

        private string GetTempFolder(RepositoryPackage package)
        {
            var packageFileName = Path.Combine(Settings.Default.PickupPath, package.PackageFileName);
            var fi = new FileInfo(packageFileName);
            var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(), fi.Name.Remove(fi.Name.Length - fi.Extension.Length));
            return tempFolder;
        }

        private async Task SendFailSync(ConsumeContext<PrepareForRecognitionMessage> context, string errorMessage)
        {
            // If we do have an error, we stop the sync process here.
            Log.Error("Failed to preprocess the package for archiveRecord {archiveRecordId} with conversationId {ConversationId}",
                context.Message.ArchiveRecord.ArchiveRecordId, context.ConversationId);
            await assetManager.UpdatePrimaerdatenAuftragStatus(new UpdatePrimaerdatenAuftragStatus
            {
                PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                Service = AufbereitungsServices.AssetService,
                Status = AufbereitungsStatusEnum.PreprocessingAbgeschlossen,
                ErrorText = errorMessage
            });

            await context.Publish<IArchiveRecordUpdated>(new
            {
                context.Message.MutationId,
                context.Message.ArchiveRecord.ArchiveRecordId,
                ActionSuccessful = false,
                context.Message.PrimaerdatenAuftragId,
                ErrorMessage = errorMessage
            });
            Log.Information("Put {CommandName} message on index queue with mutation ID: {MutationId}", nameof(IArchiveRecordUpdated),
                context.Message.MutationId);
        }
    }
}