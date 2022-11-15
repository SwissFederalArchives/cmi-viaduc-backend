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
    ///     Fetches IArchiveRecordRemoved messages from the bus
    /// </summary>
    /// <seealso cref="IArchiveRecordRemoved" />
    public class ArchiveRecordRemovedConsumer : IConsumer<IArchiveRecordRemoved>
    {
        private readonly IHarvestManager harvestManager;

        public ArchiveRecordRemovedConsumer(IHarvestManager harvestManager)
        {
            this.harvestManager = harvestManager;
        }

        public Task Consume(ConsumeContext<IArchiveRecordRemoved> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(IArchiveRecordRemoved),
                    context.ConversationId);

                if (context.Message.ActionSuccessful)
                {
                    harvestManager.UpdateMutationStatus(new MutationStatusInfo
                        {MutationId = context.Message.MutationId, NewStatus = ActionStatus.SyncCompleted});
                }
                else
                {
                    harvestManager.UpdateMutationStatus(new MutationStatusInfo
                    {
                        MutationId = context.Message.MutationId,
                        NewStatus = ActionStatus.SyncFailed,
                        ErrorMessage = context.Message.ErrorMessage,
                        StackTrace = context.Message.StackTrace
                    });
                }

                return Task.CompletedTask;
            }
        }
    }
}