using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Engine.Asset.PostProcess;
using CMI.Manager.Asset.Properties;
using MassTransit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Asset.Consumers
{
    public class RecognitionPostProcessingConsumer : IConsumer<RecognitionPostProcessingMessage>
    {
        private readonly IAssetManager assetManager;
        private readonly IBus bus;
        private readonly IAssetPostProcessingEngine assetPostProcessingEngine;
        private List<Func<RecognitionPostProcessingMessage, Task<ProcessStepResult>>> postPreparationSteps;


        public RecognitionPostProcessingConsumer(IAssetManager assetManager, IBus bus, IAssetPostProcessingEngine assetPostProcessingEngine)
        {
            this.assetPostProcessingEngine = assetPostProcessingEngine;
            this.assetManager = assetManager;
            this.bus = bus;
            postPreparationSteps = new List<Func<RecognitionPostProcessingMessage, Task<ProcessStepResult>>>();
        }

        public async Task Consume(ConsumeContext<RecognitionPostProcessingMessage> context)
        {
            try
            {
                using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
                {
                    Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                        nameof(RecognitionPostProcessingMessage), context.ConversationId);
                    var doViewer = await ShouldCreateViewerFiles(context.Message);
                    if (doViewer.Success)
                    {
                        // 1. Step: Convert jp2000 images to jpeg images
                        postPreparationSteps.Add(ConvertJp2ToJpeg);

                        // 2. Step: combine texts in single Page
                        postPreparationSteps.Add(CombineSinglePageTextExtractsToTextDocument);

                        // 3. Step: save extracted text
                        postPreparationSteps.Add(SaveOCRTextInSolr);

                        // 4. Step: Create IIIF manifest
                        postPreparationSteps.Add(CreateIiifManifests);

                        // 5. Step: Move IIIF files to final destination
                        postPreparationSteps.Add(DistributeIiifFiles);
                    }

                    // 6. Step: Clean up temp files
                    postPreparationSteps.Add(RemoveTemporaryFiles);

                    foreach (var step in postPreparationSteps)
                    {
                        var result = await step(context.Message);

                        // In case any step was not successful, fail the sync
                        if (!result.Success)
                        {
                            await SendFailSync(context, result.ErrorMessage);
                            // In any case remove the temp files
                            await RemoveTemporaryFiles(context.Message);
                            return;
                        }
                    }

                    await assetManager.UpdatePrimaerdatenAuftragStatus(new UpdatePrimaerdatenAuftragStatus
                    {
                        PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                        Service = AufbereitungsServices.AssetService,
                        Status = AufbereitungsStatusEnum.PostprocessingAbgeschlossen
                    });

                    // Put the final message on the queue for indexing.
                    // Important: use bus address here, because we are in SSZ and the original message comes
                    // from the BV-Zone
                    var ep = await context.GetSendEndpoint(new Uri(bus.Address, BusConstants.IndexManagerUpdateArchiveRecordMessageQueue));
                    await ep.Send<IUpdateArchiveRecord>(new
                    {
                        context.Message.MutationId,
                        context.Message.ArchiveRecord,
                        context.Message.PrimaerdatenAuftragId
                    });
                    Log.Information("Put {CommandName} message on index queue with mutation ID: {MutationId}", nameof(IUpdateArchiveRecord),
                        context.Message.MutationId);
                }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in {moduleName} for archive record id {archiveRecordId}.",
                    nameof(RecognitionPostProcessingConsumer),
                    context.Message.ArchiveRecord.ArchiveRecordId);

                await SendFailSync(context, ex.Message);
            }
        }

        private Task<ProcessStepResult> ShouldCreateViewerFiles(RecognitionPostProcessingMessage message)
        {
            var createManifestAllowed = message.ArchiveRecord.Security.PrimaryDataDownloadAccessToken.Contains(AccessRoles.RoleOe2) &&
                                        message.ArchiveRecord.Security.PrimaryDataFulltextAccessToken.Contains(AccessRoles.RoleOe2);

            if (createManifestAllowed || Settings.Default.IgnoreAccessTokensForManifestCheck)
            {
                return assetPostProcessingEngine.ContainsOnlyValidFileTypes(Settings.Default.PickupPath, message.ArchiveRecord);
            }

            return Task.FromResult(
                new ProcessStepResult
                {
                    Success = false
                });
        }

        private async Task SendFailSync(ConsumeContext<RecognitionPostProcessingMessage> context, string errorText)
        {
            Log.Error("Failed to post process the package for archiveRecord {archiveRecordId} with conversationId {ConversationId}",
                context.Message.ArchiveRecord.ArchiveRecordId, context.ConversationId);

            await assetManager.UpdatePrimaerdatenAuftragStatus(new UpdatePrimaerdatenAuftragStatus
            {
                PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                Service = AufbereitungsServices.AssetService,
                Status = AufbereitungsStatusEnum.PostprocessingAbgeschlossen,
                ErrorText = errorText
            });

            await context.Publish<IArchiveRecordUpdated>(new
            {
                context.Message.MutationId,
                context.Message.ArchiveRecord.ArchiveRecordId,
                ActionSuccessful = false,
                context.Message.PrimaerdatenAuftragId,
                ErrorMessage = errorText
            });
            Log.Information("Put {CommandName} message on index queue with mutation ID: {MutationId}", nameof(IArchiveRecordUpdated),
                context.Message.MutationId);
        }

        private Task<ProcessStepResult> ConvertJp2ToJpeg(RecognitionPostProcessingMessage message)
        {
            return assetPostProcessingEngine.ConvertJp2ToJpeg(Settings.Default.PickupPath, message.ArchiveRecord);
        }

        private Task<ProcessStepResult> CombineSinglePageTextExtractsToTextDocument(RecognitionPostProcessingMessage message)
        {
            return assetPostProcessingEngine.CombineSinglePageTextExtractsToTextDocument(Settings.Default.PickupPath, message.ArchiveRecord);
        }

        private Task<ProcessStepResult> SaveOCRTextInSolr(RecognitionPostProcessingMessage message)
        {
            return assetPostProcessingEngine.SaveOCRTextInSolr(Settings.Default.PickupPath, message.ArchiveRecord);
        }

        private Task<ProcessStepResult> CreateIiifManifests(RecognitionPostProcessingMessage message)
        {
            return assetPostProcessingEngine.CreateIiifManifests(Settings.Default.PickupPath, message.ArchiveRecord);
        }

        private Task<ProcessStepResult> DistributeIiifFiles(RecognitionPostProcessingMessage message)
        {
            return assetPostProcessingEngine.DistributeIiifFiles(Settings.Default.PickupPath, message.ArchiveRecord);
        }

        private async Task<ProcessStepResult> RemoveTemporaryFiles(RecognitionPostProcessingMessage message)
        {
            var packageFileName = message.ArchiveRecord.PrimaryData[0].PackageFileName;
            var primaerdatenAuftragId = message.PrimaerdatenAuftragId;

            var success = await assetManager.RemoveTemporaryFiles(new ExtractZipArgument
            {
                PackageFileName = packageFileName,
                PrimaerdatenAuftragId = primaerdatenAuftragId
            });

            return success
                ? new ProcessStepResult {Success = true}
                : new ProcessStepResult {Success = false, ErrorMessage = $"Failed to remove temporary files {packageFileName}"};
        }
    }
}