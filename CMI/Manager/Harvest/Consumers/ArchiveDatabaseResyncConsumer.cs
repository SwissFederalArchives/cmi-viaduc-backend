using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Harvest.Consumers
{
    public class ArchiveDatabaseResyncConsumer : IConsumer<IResyncArchiveDatabase>
    {
        private readonly IHarvestManager harvestManager;

        public ArchiveDatabaseResyncConsumer(IHarvestManager harvestManager)
        {
            this.harvestManager = harvestManager;
        }

        public async Task Consume(ConsumeContext<IResyncArchiveDatabase> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(IResyncArchiveDatabase),
                    context.ConversationId);

                var affectedRecords = harvestManager.InitiateFullResync(context.Message.RequestInfo);
                await context.Publish<IResyncArchiveDatabaseStarted>(new {InsertedRecords = affectedRecords});
            }
        }
    }
}