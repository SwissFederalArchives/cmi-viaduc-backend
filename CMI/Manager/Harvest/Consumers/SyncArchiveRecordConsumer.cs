using System;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using CMI.Contract.Messaging;
using CMI.Manager.Harvest.Infrastructure;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Harvest.Consumers
{
    /// <summary>
    ///     Fetches ISyncArchiveRecord commands from the bus
    /// </summary>
    /// <seealso cref="ISyncArchiveRecord" />
    public class SyncArchiveRecordConsumer : IConsumer<ISyncArchiveRecord>
    {
        private readonly ICachedHarvesterSetting cachedSettings;
        private readonly IRequestClient<FindArchiveRecordRequest> findArchiveRecordClient;
        private readonly IHarvestManager harvestManager;


        /// <summary>
        ///     Initializes a new instance of the <see cref="SyncArchiveRecordConsumer" /> class.
        /// </summary>
        /// <param name="harvestManager">The harvest manager responsible for sync management.</param>
        public SyncArchiveRecordConsumer(IHarvestManager harvestManager,
            IRequestClient<FindArchiveRecordRequest> findArchiveRecordClient, ICachedHarvesterSetting cachedSettings)
        {
            this.harvestManager = harvestManager;
            this.findArchiveRecordClient = findArchiveRecordClient;
            this.cachedSettings = cachedSettings;
        }

        /// <summary>
        ///     Consumes the specified message from the bus.
        /// </summary>
        /// <param name="context">The context from the bus.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.NotSupportedException"></exception>
        public async Task Consume(ConsumeContext<ISyncArchiveRecord> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(ISyncArchiveRecord),
                    context.ConversationId);

                var message = context.Message;
                switch (message.Action.ToLowerInvariant())
                {
                    case "update":
                        var archiveRecord = harvestManager.BuildArchiveRecord(message.ArchiveRecordId);

                        // If no records was found it could be, that the records was deleted or put into 
                        // status "in Bearbeitung" after it was put on the queue. In this case we end
                        // sync process
                        if (archiveRecord == null)
                        {
                            harvestManager.UpdateMutationStatus(new MutationStatusInfo
                            {
                                MutationId = context.Message.MutationId,
                                NewStatus = ActionStatus.SyncAborted,
                                ChangeFromStatus = ActionStatus.SyncInProgress,
                                ErrorMessage = "Record was not found in the database anymore. Might have been deleted in the meantime."
                            });
                            return;
                        }

                        // Security Check
                        // If no Metadata Access Token is present, then we end the sync process here,
                        // as this record MUST not be synced to Viaduc
                        if (!archiveRecord.Security.MetadataAccessToken.Any())
                        {
                            harvestManager.UpdateMutationStatus(new MutationStatusInfo
                            {
                                MutationId = context.Message.MutationId,
                                NewStatus = ActionStatus.SyncAborted,
                                ChangeFromStatus = ActionStatus.SyncInProgress,
                                ErrorMessage =
                                    "Record can not be synced to Viaduc due to it's security level. Record should not have entered the sync queue in the first place."
                            });
                            return;
                        }

                        // Fetch the (eventally) existing archive record
                        var elasticRecord = await GetElasticArchiveRecord(archiveRecord.ArchiveRecordId);

                        // Does the AIS data provide a primary data link?
                        if (string.IsNullOrEmpty(archiveRecord.Metadata.PrimaryDataLink))
                        {
                            // Did the old record have a primary data link?
                            if (elasticRecord != null && !string.IsNullOrEmpty(elasticRecord.PrimaryDataLink))
                            {
                                var epDel = await context.GetSendEndpoint(new Uri(context.SourceAddress, BusConstants.CacheDeleteFile));
                                await epDel.Send<IDeleteFileFromCache>(new
                                {
                                    archiveRecord.ArchiveRecordId
                                });
                                Log.Information("Put {CommandName} message on cache queue with mutation ID: {MutationId}",
                                    nameof(IDeleteFileFromCache), context.Message.MutationId);
                            }

                            await UpdateArchiveRecord(context, message, archiveRecord);
                        }
                        else
                        {
                            // Is the primary data of the existing elastic record and the ais record the same?
                            // And is the full resync option NOT set
                            if (elasticRecord != null && elasticRecord.PrimaryDataLink == archiveRecord.Metadata.PrimaryDataLink &&
                                !cachedSettings.EnableFullResync())
                            {
                                // Add the primary data from the existing record to the new ais data
                                archiveRecord.ElasticPrimaryData = elasticRecord.PrimaryData;
                                await UpdateArchiveRecord(context, message, archiveRecord);
                            }
                            else
                            {
                                var ep = await context.GetSendEndpoint(new Uri(context.SourceAddress,
                                    BusConstants.RepositoryManagerReadPackageMetadataMessageQueue));
                                await ep.Send<IArchiveRecordAppendPackageMetadata>(new
                                {
                                    message.MutationId,
                                    ArchiveRecord = archiveRecord,
                                    ElasticRecord = elasticRecord
                                });
                                Log.Information("Put {CommandName} message on repository queue queue with mutation ID: {MutationId}",
                                    nameof(IScheduleForPackageSync), context.Message.MutationId);
                            }
                        }

                        break;
                    case "delete":
                        var epDelete = await context.GetSendEndpoint(new Uri(context.SourceAddress,
                            BusConstants.IndexManagerRemoveArchiveRecordMessageQueue));
                        await epDelete.Send<IRemoveArchiveRecord>(new
                        {
                            message.MutationId,
                            message.ArchiveRecordId
                        });
                        Log.Information("Put {CommandName} message on index queue with mutation ID: {MutationId}", nameof(IRemoveArchiveRecord),
                            context.Message.MutationId);
                        break;
                    default:
                        throw new NotSupportedException($"The action: {message.Action} is not a supported action name!");
                }
            }
        }

        private static async Task UpdateArchiveRecord(ConsumeContext<ISyncArchiveRecord> context, ISyncArchiveRecord message,
            ArchiveRecord archiveRecord)
        {
            var ep = await context.GetSendEndpoint(new Uri(context.SourceAddress, BusConstants.IndexManagerUpdateArchiveRecordMessageQueue));
            await ep.Send<IUpdateArchiveRecord>(new
            {
                message.MutationId,
                ArchiveRecord = archiveRecord
            });
            Log.Information("Put {CommandName} message on index queue with mutation ID: {MutationId}", nameof(IUpdateArchiveRecord),
                context.Message.MutationId);
        }

        private async Task<ElasticArchiveRecord> GetElasticArchiveRecord(string archiveRecordId)
        {
            try
            {
                var result = await findArchiveRecordClient.GetResponse<FindArchiveRecordResponse>(new FindArchiveRecordRequest
                    {ArchiveRecordId = archiveRecordId, IncludeFulltextContent = true});
                return result.Message.ElasticArchiveRecord;
            }
            catch (Exception e)
            {
                Log.Error(e, $"Unexpected error fetching archive record from index with id {archiveRecordId}.");
                return null;
            }
        }
    }
}