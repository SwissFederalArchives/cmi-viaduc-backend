using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Asset.Consumers
{
    public class ExtractFulltextPackageConsumer : IConsumer<IArchiveRecordExtractFulltextFromPackage>
    {
        private readonly IAssetManager assetManager;
        private readonly IBus bus;

        public ExtractFulltextPackageConsumer(IAssetManager assetManager, IBus bus)
        {
            this.assetManager = assetManager;
            this.bus = bus;
        }

        public async Task Consume(ConsumeContext<IArchiveRecordExtractFulltextFromPackage> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(IArchiveRecordExtractFulltextFromPackage), context.ConversationId);

                // Process the package and extract the full text
                var mutationId = context.Message.MutationId;
                var archiveRecord = context.Message.ArchiveRecord;
                var primaerdatenAuftragId = context.Message.PrimaerdatenAuftragId;
                var success = await assetManager.ExtractFulltext(mutationId, archiveRecord, primaerdatenAuftragId);

                if (success)
                {
                    await UpdatePrimaerdatenAuftragStatus(context.Message, AufbereitungsServices.AssetService,
                        AufbereitungsStatusEnum.OCRAbgeschlossen);

                    // Put the final message on the queue for indexing.
                    // Important: use bus address here, because we are in SSZ and the original message comes
                    // from the BV-Zone
                    var ep = await context.GetSendEndpoint(new Uri(bus.Address, BusConstants.IndexManagerUpdateArchiveRecordMessageQueue));
                    await ep.Send<IUpdateArchiveRecord>(new
                    {
                        MutationId = mutationId,
                        ArchiveRecord = archiveRecord,
                        PrimaerdatenAuftragId = primaerdatenAuftragId
                    });
                    Log.Information("Put {CommandName} message on index queue with mutation ID: {MutationId}", nameof(IUpdateArchiveRecord),
                        mutationId);
                }
                else
                {
                    // If package creation was not successful, stop syncing here and return failure.
                    Log.Error("Failed to extract fulltext for the supported file types for archiveRecord with conversationId {ConversationId}",
                        context.ConversationId);
                    var errorText = "Failed to extract fulltext. See log for further details.";
                    await UpdatePrimaerdatenAuftragStatus(context.Message, AufbereitungsServices.AssetService,
                        AufbereitungsStatusEnum.OCRAbgeschlossen, errorText);

                    await context.Publish<IArchiveRecordUpdated>(new
                    {
                        context.Message.MutationId,
                        context.Message.ArchiveRecord.ArchiveRecordId,
                        ActionSuccessful = false,
                        PrimaerdatenAuftragId = primaerdatenAuftragId,
                        ErrorMessage = errorText
                    });
                    Log.Information("Put {CommandName} message on index queue with mutation ID: {MutationId}", nameof(IArchiveRecordUpdated),
                        mutationId);
                }
            }
        }

        private async Task UpdatePrimaerdatenAuftragStatus(IArchiveRecordExtractFulltextFromPackage message, AufbereitungsServices service,
            AufbereitungsStatusEnum newStatus, string errorText = null)
        {
            if (message.PrimaerdatenAuftragId > 0)
            {
                Log.Information("Auftrag mit Id {PrimaerdatenAuftragId} wurde im {service}-Service auf Status {Status} gesetzt.",
                    message.PrimaerdatenAuftragId, service.ToString(), newStatus.ToString());

                var ep = await bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.AssetManagerUpdatePrimaerdatenAuftragStatusMessageQueue));
                await ep.Send<IUpdatePrimaerdatenAuftragStatus>(new UpdatePrimaerdatenAuftragStatus
                {
                    PrimaerdatenAuftragId = message.PrimaerdatenAuftragId,
                    Service = service,
                    Status = newStatus,
                    ErrorText = errorText
                });
            }
        }
    }
}