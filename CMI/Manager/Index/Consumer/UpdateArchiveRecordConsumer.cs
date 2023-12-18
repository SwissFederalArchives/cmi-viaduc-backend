using System;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Index.Consumer
{
    /// <summary>
    ///     Class UpdateArchiveRecordConsumer.
    /// </summary>
    /// <seealso cref="IUpdateArchiveRecord" />
    public class UpdateArchiveRecordConsumer : IConsumer<IUpdateArchiveRecord>
    {
        private readonly IIndexManager indexManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UpdateArchiveRecordConsumer" /> class.
        /// </summary>
        /// <param name="indexManager">The index manager that is responsible for updating.</param>
        public UpdateArchiveRecordConsumer(IIndexManager indexManager)
        {
            this.indexManager = indexManager;
        }

        /// <summary>
        ///     Consumes the specified message from the bus.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Task.</returns>
        public async Task Consume(ConsumeContext<IUpdateArchiveRecord> context)
        {
            Log.Information($"Updated archive record {context.Message.ArchiveRecord.ArchiveRecordId} in elastic index.");
            var currentStatus = AufbereitungsStatusEnum.OCRAbgeschlossen;
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(IUpdateArchiveRecord),
                    context.ConversationId);

                try
                {
                    var archiveRecord = context.Message.ArchiveRecord;
                    var elasticArchiveRecord = indexManager.ConvertArchiveRecord(archiveRecord);

                    // Check if record must be anonymized
                    // Is necessary if the method was not called by the anonymization consumer (ElasticArchiveDbRecord == null)
                    // and the record has FieldAccess tokens
                    if (context.Message.ElasticArchiveDbRecord == null && context.Message.ArchiveRecord.Security.FieldAccessToken.Any())
                    {
                        var ep = await context.GetSendEndpoint(new Uri(context.SourceAddress,
                            BusConstants.IndexManagerAnonymizeArchiveRecordMessageQueue));
                        await ep.Send<IAnonymizationArchiveRecord>(new
                        {
                            MutationId = context.Message.MutationId,
                            PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                            ArchiveRecord = context.Message.ArchiveRecord,
                            DoNotReportCompletion = context.Message.DoNotReportCompletion,
                            ElasticArchiveDbRecord = elasticArchiveRecord
                        });
                    }
                    // ----------------------------------------------------------------------
                    // Der Datensatz muss nicht anonymisiert werden, oder wurde anonymisiert.
                    // ----------------------------------------------------------------------
                    else
                    {
                        if (context.Message.ElasticArchiveDbRecord != null)
                        {
                            // Back from anonymize service: Update the record
                            indexManager.UpdateArchiveRecord(context.Message.ElasticArchiveDbRecord);
                        }
                        else
                        {
                            // Update the record that was generated from the passed ArchiveRecord
                            // This is the case when there are no FieldAccessTokens
                            
                            // Delete any existing manual corrections that may exist
                            indexManager.DeletePossiblyExistingManuelleKorrektur(elasticArchiveRecord);
                            indexManager.UpdateArchiveRecord(elasticArchiveRecord);
                        }

                        // In the archiveplan or the references we could have protected records that have changed
                        // until the last sync of this record. We need to update those as well to be in sync
                        UpdateAnyProtectedRelatedRecords(elasticArchiveRecord);

                        // update the individual tokens for download and access 
                        // these tokens need to be updated even if the record has no primary data.
                        // Use case: Ö2 user asks for Einsichtsgesuch. It gets approved, but record may not (yet) have primary data.
                        var ep = await context.GetSendEndpoint(new Uri(context.SourceAddress, BusConstants.RecalcIndivTokens));
                        await ep.Send(new RecalcIndivTokens
                        {
                            ArchiveRecordId = Convert.ToInt32(context.Message.ArchiveRecord.ArchiveRecordId),
                            ExistingMetadataAccessTokens = context.Message.ArchiveRecord.Security.MetadataAccessToken.ToArray(),
                            ExistingPrimaryDataDownloadAccessTokens = context.Message.ArchiveRecord.Security.PrimaryDataDownloadAccessToken.ToArray(),
                            ExistingPrimaryDataFulltextAccessTokens = context.Message.ArchiveRecord.Security.PrimaryDataFulltextAccessToken.ToArray(),
                            ExistingFieldAccessTokens = context.Message.ArchiveRecord.Security?.FieldAccessToken.ToArray()
                        });
                        Log.Information(
                            $"Recalculated and updated individual tokens for archive record {context.Message.ArchiveRecord.ArchiveRecordId} in elastic index.");

                        // When syncing a UoD with primarydata, the harvester sends
                        // a first metadata update (without PrimparyDataLink) but with
                        // the instruction not to report completion.
                        // Else the sync would be marked as completed and this
                        // this is not correct. 
                        if (!context.Message.DoNotReportCompletion)
                        {
                            currentStatus = AufbereitungsStatusEnum.IndizierungAbgeschlossen;
                            await PrimaerdatenAuftragHelper.UpdatePrimaerdatenAuftragStatus(context, AufbereitungsServices.IndexService, currentStatus);

                            await context.Publish<IArchiveRecordUpdated>(new
                            {
                                context.Message.MutationId,
                                context.Message.ArchiveRecord.ArchiveRecordId,
                                ActionSuccessful = true,
                                context.Message.PrimaerdatenAuftragId
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to update archiveRecord with conversationId {ConversationId} in Elastic or SQL", context.ConversationId);
                    if (!context.Message.DoNotReportCompletion)
                    {
                        await context.Publish<IArchiveRecordUpdated>(new
                        {
                            context.Message.MutationId,
                            context.Message.ArchiveRecord.ArchiveRecordId,
                            ActionSuccessful = false,
                            context.Message.PrimaerdatenAuftragId,
                            ErrorMessage = ex.Message,
                            ex.StackTrace
                        });
                        await PrimaerdatenAuftragHelper.UpdatePrimaerdatenAuftragStatus(context, AufbereitungsServices.IndexService, currentStatus,
                            ex.Message);
                    }
                }
            }
        }

        private void UpdateAnyProtectedRelatedRecords(ElasticArchiveRecord elasticArchiveRecord)
        {
            // If a record has a parent that is anonymized
            // (something that is rare, but can happen), then we need to fetch that parent
            // and sync the related records of that parent. This updates the archiveplan and parentContentInfos
            // of its children
            var protectedParents = elasticArchiveRecord.ArchiveplanContext.Where(a => a.Protected);
            foreach (var protectedParent in protectedParents)
            {
                indexManager.UpdateDependentRecords(protectedParent.ArchiveRecordId);
            }

            // if a record has references to UoD that are anonymized, then
            // we need to update those data as well, as those data could have been updated until the last sync
            var protectedReferences = elasticArchiveRecord.References.Where(a => a.Protected).ToList();

            // if a record has protected references, we need to make sure that the current record is updating its own references
            // This step is required, because the UpdateDependentRecords won't update the references, if this
            // record is synced for the first time and the referenced record is not yet up to date in Elastic
            if (protectedReferences.Any())
            {
                indexManager.UpdateReferencesOfUnprotectedRecord(elasticArchiveRecord.ArchiveRecordId);
                // Then sync the dependent records as well
                foreach (var protectedReference in protectedReferences)
                {
                    indexManager.UpdateDependentRecords(protectedReference.ArchiveRecordId);
                }
            }
        }
    }
}
