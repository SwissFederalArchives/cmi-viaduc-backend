using System;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Asset.Consumers
{
    public class GetAssetStatusConsumer : IConsumer<GetAssetStatusRequest>
    {
        private readonly IAssetManager assetManager;
        private readonly IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse> requestClient;

        public GetAssetStatusConsumer(IAssetManager assetManager, IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse> requestClient)
        {
            this.assetManager = assetManager;
            this.requestClient = requestClient;
        }

        public async Task Consume(ConsumeContext<GetAssetStatusRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(GetAssetStatusRequest), context.ConversationId);
                var message = context.Message;


                // Prüfen, ob die Datei schon im Cache vorhanden ist. Wenn ja, ist alles gut.
                var response = await requestClient.Request(new DoesExistInCacheRequest
                {
                    Id = context.Message.ArchiveRecordId,
                    RetentionCategory = context.Message.RetentionCategory
                });

                if (response.Exists)
                {
                    Log.Information("Found the asset for id {ArchiveRecordId} with type {AssetType} and Size {FileSize} bytes in the cache.",
                        message.ArchiveRecordId, message.AssetType, response.FileSizeInBytes);

                    var assetStatusResult = new GetAssetStatusResult
                    {
                        Status = AssetDownloadStatus.InCache,
                        InQueueSince = DateTime.MinValue,
                        EstimatedPreparationDuration = TimeSpan.Zero,
                        EstimatedPreparationEnd = DateTime.Now,
                        FileSizeInBytes = response.FileSizeInBytes
                    };

                    Log.Information("Constructed getAssetStatusResult");
                    await context.RespondAsync(assetStatusResult);
                    return;
                }


                // Is that asset in the preparation queue?
                var status = await assetManager.CheckPreparationStatus(message.ArchiveRecordId);
                if (status.PackageIsInPreparationQueue)
                {
                    Log.Information("Found the asset for id {ArchiveRecordId} with type {AssetType} in the preparation queue.",
                        message.ArchiveRecordId, message.AssetType);
                    var assetStatusResult = new GetAssetStatusResult
                    {
                        Status = AssetDownloadStatus.InPreparationQueue,
                        InQueueSince = status.AddedToQueueOn,
                        EstimatedPreparationDuration = status.EstimatedPreparationDuration,
                        EstimatedPreparationEnd = status.AddedToQueueOn == DateTime.MinValue
                            ? DateTime.MinValue
                            : status.AddedToQueueOn.AddTicks(status.EstimatedPreparationDuration.Ticks)
                    };

                    await context.RespondAsync(assetStatusResult);
                    return;
                }

                // The asset is not available
                await context.RespondAsync(new GetAssetStatusResult
                {
                    Status = AssetDownloadStatus.RequiresPreparation,
                    InQueueSince = DateTime.MinValue,
                    EstimatedPreparationDuration = status.EstimatedPreparationDuration,
                    EstimatedPreparationEnd = status.AddedToQueueOn == DateTime.MinValue
                        ? DateTime.MinValue
                        : status.AddedToQueueOn.AddTicks(status.EstimatedPreparationDuration.Ticks)
                });
            }
        }
    }
}