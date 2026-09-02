using CMI.Access.Harvest.ActaPro;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Harvest.Infrastructure;
using MassTransit;
using Serilog;
using System;
using System.Linq;
using System.Threading.Tasks;
using LogContext = Serilog.Context.LogContext;

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
                        ArchiveRecord archiveRecord;
                        try
                        {
                            archiveRecord = await harvestManager.BuildArchiveRecord(message.ArchiveRecordId);
                        }
                        catch (ApiException apiException)
                        {
                            Log.Error(apiException.Message, "ApiException while building the archive record for id {archiveRecordId}", message.ArchiveRecordId);
                            await harvestManager.UpdateMutationStatus(new MutationStatusInfo
                            {
                                MutationId = context.Message.MutationId,
                                NewStatus = ActionStatus.SyncFailed,
                                ArchiveRecordId = message.ArchiveRecordId,
                                ChangeFromStatus = ActionStatus.SyncInProgress,
                                ErrorMessage = "Record was not found in the database anymore. Might have been deleted in the meantime."
                            });
                            return;
                        }
                        catch(Exception ex)
                        {
                            Log.Error(ex.Message, "Unexpected error while building the archive record for id {archiveRecordId}", message.ArchiveRecordId);
                            await harvestManager.UpdateMutationStatus(new MutationStatusInfo
                            {
                                MutationId = context.Message.MutationId,
                                NewStatus = ActionStatus.SyncFailed,
                                ArchiveRecordId = message.ArchiveRecordId,
                                ChangeFromStatus = ActionStatus.SyncInProgress,
                                ErrorMessage = "Record could not be synced because the ACTApro system seems to be down."
                            });
                            return;
                        }

                        // If no records was found it could be, that the records was deleted or put into 
                        // status "in Bearbeitung" after it was put on the queue. In this case we mark it as failed
                        if (archiveRecord == null)
                        {
                            await harvestManager.UpdateMutationStatus(new MutationStatusInfo
                            {
                                MutationId = context.Message.MutationId,
                                NewStatus = ActionStatus.SyncFailed,
                                ArchiveRecordId = message.ArchiveRecordId,
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
                            await harvestManager.UpdateMutationStatus(new MutationStatusInfo
                            {
                                MutationId = context.Message.MutationId,
                                NewStatus = ActionStatus.SyncAborted,
                                ChangeFromStatus = ActionStatus.SyncInProgress,
                                ArchiveRecordId = message.ArchiveRecordId,
                                ErrorMessage =
                                    "Record can not be synced to Viaduc due to it's security level. Record should not have entered the sync queue in the first place."
                            });
                            return;
                        }

                        // Fetch the (eventually) existing archive record
                        var elasticRecord = await GetElasticArchiveRecord(archiveRecord.ArchiveRecordId, MetadataToExclude.OCRContentAndFiles);

                        var deleteOldScopeRecord = false;  
                        if (archiveRecord.Metadata.DetailData.Any(d => d.ElementName == "ScopeID") 
                            && archiveRecord.Metadata.DetailData.First(d => d.ElementName == "ScopeID").ElementValue?.Count  > 0)
                        {
                            var scopeId = archiveRecord.Metadata.DetailData.First(d => d.ElementName == "ScopeID").ElementValue[0]?.TextValues[0].Value;
                            elasticRecord = await GetElasticArchiveRecord(scopeId, MetadataToExclude.OCRContentAndFiles);
                            // Make sure, that we really got the record with the scopeId.
                            // If the record already is converted, the UUID is returned, even when we search for the scopeId
                            if (elasticRecord != null && elasticRecord.ArchiveRecordId == scopeId)
                            {
                                deleteOldScopeRecord = true;
                            }
                        }

                        if (elasticRecord != null && !(archiveRecord.Security.PrimaryDataDownloadAccessToken.Contains(AccessRoles.RoleOe2) &&
                                                       archiveRecord.Security.PrimaryDataFulltextAccessToken.Contains(AccessRoles.RoleOe2)))
                        {
                            await DeleteProcessedPrimarydata(context, elasticRecord);
                            // Also remove the manifest link, as the Primarydata a has been deleted.
                            elasticRecord.ManifestLink = null;
                        }

                        // Does the AIS data provide a primary data link?
                        if (string.IsNullOrEmpty(archiveRecord.Metadata.PrimaryDataLink))
                        {
                            // Did the old record have a primary data link?
                            if (elasticRecord != null && !string.IsNullOrEmpty(elasticRecord.PrimaryDataLink))
                            {
                                await DeleteProcessedPrimarydata(context, elasticRecord);
                            }

                            await UpdateArchiveRecord(context, message, archiveRecord, false, deleteOldScopeRecord);
                        }
                        else
                        {
                            // Is the primary data of the existing elastic record and the ais record the same?
                            // And is the full resync option NOT set
                            if (elasticRecord != null && elasticRecord.PrimaryDataLink == archiveRecord.Metadata.PrimaryDataLink &&
                                     !cachedSettings.EnableFullResync())
                            {
                                // Add the primary data from the existing record to the new ais data
                                var elastivRecordWithPrimaryData = await GetElasticArchiveRecord(elasticRecord.ArchiveRecordId, MetadataToExclude.Nothing);
                                archiveRecord.ElasticPrimaryData = elastivRecordWithPrimaryData?.PrimaryData;
                                // Also add the manifest link, or we loose it
                                archiveRecord.Metadata.ManifestLink = elasticRecord.ManifestLink;
                                await UpdateArchiveRecord(context, message, archiveRecord, false, deleteOldScopeRecord);
                            }
                            else
                            {
                                // So we have to do a sync with primary data
                                // As the export of the DIR does rely on metadata that is fetched from elastic,
                                // we are going to update/insert the archive record in Elastic (but without the extracted OCR first)
                                // The update in the index should be done quickly, so when the data is needed from the DIR it will be available

                                // First also copy an eventually existing manifest link, so the current data is still available to the user
                                archiveRecord.Metadata.ManifestLink = elasticRecord?.ManifestLink;
                                await UpdateArchiveRecord(context, message, archiveRecord, true, deleteOldScopeRecord);

                                // Now start getting the metadata info of the DIR package
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
                        elasticRecord = await GetElasticArchiveRecord(message.ArchiveRecordId, MetadataToExclude.OCRContentAndFiles);
                        if (elasticRecord != null)
                        {
                            await DeleteProcessedPrimarydata(context, elasticRecord);
                            var epDelete = await context.GetSendEndpoint(new Uri(context.SourceAddress,
                                BusConstants.IndexManagerRemoveArchiveRecordMessageQueue));
                            await epDelete.Send<IRemoveArchiveRecord>(new
                            {
                                message.MutationId,
                                message.ArchiveRecordId
                            });
                            Log.Information("Put {CommandName} message on index queue with mutation ID: {MutationId}", nameof(IRemoveArchiveRecord),
                                context.Message.MutationId);
                        }
                        else
                        {
                            await harvestManager.UpdateMutationStatus(new MutationStatusInfo
                            {
                                MutationId = context.Message.MutationId,
                                NewStatus = ActionStatus.SyncAborted,
                                ArchiveRecordId = message.ArchiveRecordId,
                                ChangeFromStatus = ActionStatus.SyncInProgress,
                                ErrorMessage = "Record could not be deleted because the archive record was not found in Elastic."
                            });
                        }
                        break;
                    default:
                        throw new NotSupportedException($"The action: {message.Action} is not a supported action name!");
                }
            }
        }

        private static async Task DeleteProcessedPrimarydata(ConsumeContext<ISyncArchiveRecord> context,
            ElasticArchiveRecord elasticRecord)
        {
            var epDelCache = await context.GetSendEndpoint(new Uri(context.SourceAddress, BusConstants.CacheDeleteFile));
            await epDelCache.Send<IDeleteFileFromCache>(new
            {
                elasticRecord.ArchiveRecordId
            });

            Log.Information("Put {CommandName} message on cache queue with mutation ID: {MutationId}",
                nameof(IDeleteFileFromCache), context.Message.MutationId);
            if (!string.IsNullOrWhiteSpace(elasticRecord.ManifestLink))
            {
                var epDelIiif = await context.GetSendEndpoint(new Uri(context.SourceAddress, BusConstants.AssetManagerDeleteViewerFiles));
                await epDelIiif.Send<IDeleteViewerFiles>(new
                {
                    elasticRecord.ManifestLink
                });

                Log.Information("Put {CommandName} message on cache queue with mutation ID: {MutationId}",
                    nameof(IDeleteViewerFiles), context.Message.MutationId);
            }
        }

        private static async Task UpdateArchiveRecord(ConsumeContext<ISyncArchiveRecord> context, ISyncArchiveRecord message,
            ArchiveRecord archiveRecord, bool doNotReportCompletion, bool recordIdToBeDeleted)
        {
            var ep = await context.GetSendEndpoint(new Uri(context.SourceAddress, BusConstants.IndexManagerUpdateArchiveRecordMessageQueue));
            await ep.Send<IUpdateArchiveRecord>(new
            {
                message.MutationId,
                ArchiveRecord = archiveRecord,
                doNotReportCompletion,
                recordIdToBeDeleted
            });
            Log.Information("Put {CommandName} message on index queue with mutation ID: {MutationId}", nameof(IUpdateArchiveRecord),
                context.Message.MutationId);
        }

        private async Task<ElasticArchiveRecord> GetElasticArchiveRecord(string archiveRecordId, MetadataToExclude metadataToExclude)
        {
            try
            {
                var result = await findArchiveRecordClient.GetResponse<FindArchiveRecordResponse>(new FindArchiveRecordRequest
                { ArchiveRecordId = archiveRecordId, MetadataToExclude = metadataToExclude });
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