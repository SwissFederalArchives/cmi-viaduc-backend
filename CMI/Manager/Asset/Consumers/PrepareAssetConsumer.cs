using System;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Asset.Consumers
{
    public class PrepareAssetConsumer : IConsumer<PrepareAssetRequest>
    {
        private readonly IAssetManager assetManager;
        private readonly IBus bus;
        private readonly IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse> indexClient;

        public PrepareAssetConsumer(IAssetManager assetManager, IBus bus,
            IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse> indexClient)
        {
            this.assetManager = assetManager;
            this.bus = bus;
            this.indexClient = indexClient;
        }

        public async Task Consume(ConsumeContext<PrepareAssetRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(PrepareAssetRequest), context.ConversationId);
                var message = context.Message;

                // Is that asset in the preparation queue?
                var status = await assetManager.CheckPreparationStatus(message.ArchiveRecordId);
                if (status.PackageIsInPreparationQueue)
                {
                    Log.Information("Found the asset for id {ArchiveRecordId} with type {AssetType} in the preparation queue.",
                        message.ArchiveRecordId, message.AssetType);
                    var prepareAssetResult = new PrepareAssetResult
                    {
                        Status = AssetDownloadStatus.InPreparationQueue,
                        InQueueSince = status.AddedToQueueOn,
                        EstimatedPreparationDuration = status.EstimatedPreparationDuration,
                        EstimatedPreparationEnd = status.AddedToQueueOn.AddTicks(status.EstimatedPreparationDuration.Ticks)
                    };

                    await context.RespondAsync(prepareAssetResult);
                    return;
                }

                // Asset is not in preperation queue, 
                // so we start the process of downloading the package and then transforming it
                switch (message.AssetType)
                {
                    case AssetType.Gebrauchskopie:
                        Log.Information("Start fetching usage copy for id {ArchiveRecordId} with type {AssetType}.", message.ArchiveRecordId,
                            message.AssetType);

                        var archiveRecord = await indexClient.Request(new FindArchiveRecordRequest {ArchiveRecordId = message.ArchiveRecordId});
                        // Register the job in the queue
                        var auftragId = await assetManager.RegisterJobInPreparationQueue(message.ArchiveRecordId, message.AssetId,
                            AufbereitungsArtEnum.Download, AufbereitungsServices.AssetService, archiveRecord.ElasticArchiveRecord.PrimaryData,
                            new DownloadPackage
                            {
                                PackageId = message.AssetId,
                                CallerId = message.CallerId,
                                ArchiveRecordId = message.ArchiveRecordId,
                                RetentionCategory = message.RetentionCategory,
                                Recipient = context.Message.Recipient,
                                DeepLinkToVe = context.Message.DeepLinkToVe
                            });

                        await context.RespondAsync(new PrepareAssetResult
                        {
                            Status = AssetDownloadStatus.InPreparationQueue,
                            InQueueSince = DateTime.Now,
                            EstimatedPreparationDuration = status.EstimatedPreparationDuration,
                            EstimatedPreparationEnd = status.AddedToQueueOn.AddTicks(status.EstimatedPreparationDuration.Ticks)
                        });
                        break;
                    case AssetType.Benutzungskopie:
                        throw new ArgumentOutOfRangeException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}