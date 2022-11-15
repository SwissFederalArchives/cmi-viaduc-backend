using System;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Harvest;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Harvest.Consumers
{
    /// <summary>
    ///     Fetches IArchiveRecordUpdated commands from the bus
    /// </summary>
    /// <seealso cref="IArchiveRecordUpdated" />
    public class ArchiveRecordUpdatedConsumer : IConsumer<IArchiveRecordUpdated>
    {
        private readonly IHarvestManager harvestManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ArchiveRecordUpdatedConsumer" /> class.
        /// </summary>
        /// <param name="harvestManager">The harvest manager responsible for sync management.</param>
        public ArchiveRecordUpdatedConsumer(IHarvestManager harvestManager)
        {
            this.harvestManager = harvestManager;
        }

        /// <summary>
        ///     Consumes the specified message from the bus.
        /// </summary>
        /// <param name="context">The context from the bus.</param>
        /// <returns>Task.</returns>
        public async Task Consume(ConsumeContext<IArchiveRecordUpdated> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} event with conversationId {ConversationId} from the bus", nameof(IArchiveRecordUpdated),
                    context.ConversationId);

                // Mark the job as done in the Auftragstabelle
                await UpdatePrimaerdatenAuftragStatus(context);

                if (context.Message.ActionSuccessful)
                {
                    harvestManager.UpdateMutationStatus(new MutationStatusInfo
                    {
                        MutationId = context.Message.MutationId,
                        NewStatus = ActionStatus.SyncCompleted,
                        ChangeFromStatus = ActionStatus.SyncInProgress
                    });
                }
                else
                {
                    harvestManager.UpdateMutationStatus(new MutationStatusInfo
                    {
                        MutationId = context.Message.MutationId,
                        NewStatus = ActionStatus.SyncFailed,
                        ChangeFromStatus = ActionStatus.SyncInProgress,
                        ErrorMessage = context.Message.ErrorMessage,
                        StackTrace = context.Message.StackTrace
                    });
                }
            }
        }

        private static async Task UpdatePrimaerdatenAuftragStatus(ConsumeContext<IArchiveRecordUpdated> context)
        {
            if (context.Message.PrimaerdatenAuftragId > 0)
            {
                await Task.Delay(3000);
                var status = AufbereitungsStatusEnum.AuftragErledigt;
                Log.Information("Auftrag mit Id {PrimaerdatenAuftragId} wurde im Harvester-Service auf Status {Status} gesetzt.",
                    context.Message.PrimaerdatenAuftragId, status.ToString());

                var ep = await context.GetSendEndpoint(new Uri(context.SourceAddress,
                    BusConstants.AssetManagerUpdatePrimaerdatenAuftragStatusMessageQueue));
                await ep.Send<IUpdatePrimaerdatenAuftragStatus>(new UpdatePrimaerdatenAuftragStatus
                {
                    PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                    Service = AufbereitungsServices.HarvestService,
                    Status = status,
                    ErrorText = context.Message.ErrorMessage
                });
            }
        }
    }
}