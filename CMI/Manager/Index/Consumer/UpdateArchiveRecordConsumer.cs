using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using Serilog.Context;

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
            var currentStatus = AufbereitungsStatusEnum.OCRAbgeschlossen;
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(IUpdateArchiveRecord),
                    context.ConversationId);

                try
                {
                    indexManager.UpdateArchiveRecord(context);
                    Log.Information($"Updated archive record {context.Message.ArchiveRecord.ArchiveRecordId} in elastic index.");

                    currentStatus = AufbereitungsStatusEnum.IndizierungAbgeschlossen;
                    await UpdatePrimaerdatenAuftragStatus(context, AufbereitungsServices.IndexService, currentStatus);

                    // update the individual tokens for download and access 
                    // these tokens need to be updated even if the record has no primary data.
                    // Use case: Ö2 user asks for Einsichtsgesuch. It gets approved, but record may not (yet) have primary data.
                    var ep = await context.GetSendEndpoint(new Uri(context.SourceAddress, BusConstants.RecalcIndivTokens));
                    await ep.Send(new RecalcIndivTokens
                    {
                        ArchiveRecordId = Convert.ToInt32(context.Message.ArchiveRecord.ArchiveRecordId),
                        ExistingMetadataAccessTokens = context.Message.ArchiveRecord.Security.MetadataAccessToken.ToArray(),
                        ExistingPrimaryDataDownloadAccessTokens = context.Message.ArchiveRecord.Security.PrimaryDataDownloadAccessToken.ToArray(),
                        ExistingPrimaryDataFulltextAccessTokens = context.Message.ArchiveRecord.Security.PrimaryDataFulltextAccessToken.ToArray()
                    });
                    Log.Information(
                        $"Recalculated and updated individual tokens for archive record {context.Message.ArchiveRecord.ArchiveRecordId} in elastic index.");

                    await context.Publish<IArchiveRecordUpdated>(new
                    {
                        context.Message.MutationId,
                        context.Message.ArchiveRecord.ArchiveRecordId,
                        ActionSuccessful = true,
                        context.Message.PrimaerdatenAuftragId
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to update archiveRecord with conversationId {ConversationId} in Elastic or SQL", context.ConversationId);
                    await context.Publish<IArchiveRecordUpdated>(new
                    {
                        context.Message.MutationId,
                        context.Message.ArchiveRecord.ArchiveRecordId,
                        ActionSuccessful = false,
                        context.Message.PrimaerdatenAuftragId,
                        ErrorMessage = ex.Message,
                        ex.StackTrace
                    });
                    await UpdatePrimaerdatenAuftragStatus(context, AufbereitungsServices.IndexService, currentStatus, ex.Message);
                }
            }
        }

        private async Task UpdatePrimaerdatenAuftragStatus(ConsumeContext<IUpdateArchiveRecord> context, AufbereitungsServices service,
            AufbereitungsStatusEnum newStatus, string errorText = null)
        {
            if (context.Message.PrimaerdatenAuftragId > 0)
            {
                Log.Information("Auftrag mit Id {PrimaerdatenAuftragId} wurde im {service}-Service auf Status {Status} gesetzt.",
                    context.Message.PrimaerdatenAuftragId, service.ToString(), newStatus.ToString());

                var ep = await context.GetSendEndpoint(new Uri(context.SourceAddress,
                    BusConstants.AssetManagerUpdatePrimaerdatenAuftragStatusMessageQueue));
                await ep.Send<IUpdatePrimaerdatenAuftragStatus>(new UpdatePrimaerdatenAuftragStatus
                {
                    PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                    Service = service,
                    Status = newStatus,
                    ErrorText = errorText
                });
            }
        }
    }
}