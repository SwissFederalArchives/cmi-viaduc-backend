using System;
using MassTransit;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using Serilog;
using CMI.Contract.Common;

namespace CMI.Manager.Index.Consumer
{
    public class AnonymizationArchiveRecordConsumer : IConsumer<IAnonymizationArchiveRecord>
    {
        private readonly IIndexManager indexManager;

        public AnonymizationArchiveRecordConsumer(IIndexManager indexManager)
        {
            this.indexManager = indexManager;
        }

        /// <summary>
        ///     Consumes the specified message from the bus.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Task.</returns>
        public async Task Consume(ConsumeContext<IAnonymizationArchiveRecord> context)
        {
            Log.Information("Received Anonymization command with MutationId {MutationId}, PrimaerdatenAuftragId {PrimaerdatenAuftragId} and ArchiveRecordID {ArchiveRecordID} from the bus",
                context.Message.MutationId, context.Message.PrimaerdatenAuftragId, context.Message.ArchiveRecord.ArchiveRecordId);

            try
            {
                var elasticArchiveRecord = await indexManager.AnonymizeArchiveRecordAsync(context.Message.ElasticArchiveDbRecord);
                elasticArchiveRecord = indexManager.SyncAnonymizedTextsWithRelatedRecords(elasticArchiveRecord);
                var ep = await context.GetSendEndpoint(new Uri(context.SourceAddress,
                    BusConstants.IndexManagerUpdateArchiveRecordMessageQueue));
                await ep.Send<IUpdateArchiveRecord>(new
                {
                    MutationId = context.Message.MutationId,
                    PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                    ArchiveRecord = context.Message.ArchiveRecord,
                    DoNotReportCompletion = context.Message.DoNotReportCompletion,
                    ElasticArchiveDbRecord = elasticArchiveRecord
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to anonymize archiveRecord {archiveRecordId} with MutationId {MutationId} and PrimaerdatenAuftragId {PrimaerdatenAuftragId} ", context.Message.ArchiveRecord.ArchiveRecordId, context.Message.MutationId, context.Message.PrimaerdatenAuftragId);
                await context.Publish<IArchiveRecordUpdated>(new
                {
                    context.Message.MutationId,
                    context.Message.ArchiveRecord.ArchiveRecordId,
                    ActionSuccessful = false,
                    context.Message.PrimaerdatenAuftragId,
                    ErrorMessage = ex.Message,
                    ex.StackTrace
                });
                await PrimaerdatenAuftragHelper.UpdatePrimaerdatenAuftragStatus(context, AufbereitungsServices.IndexService, AufbereitungsStatusEnum.OCRAbgeschlossen, ex.Message);
            }

        }
    }
}
