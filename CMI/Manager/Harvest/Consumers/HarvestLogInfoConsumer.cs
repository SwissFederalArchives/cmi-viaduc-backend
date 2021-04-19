using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Harvest.Consumers
{
    public class HarvestLogInfoConsumer : IConsumer<GetHarvestLogInfo>
    {
        private readonly IHarvestManager harvestManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="HarvestLogInfoConsumer" /> class.
        /// </summary>
        /// <param name="harvestManager">The harvest manager.</param>
        public HarvestLogInfoConsumer(IHarvestManager harvestManager)
        {
            this.harvestManager = harvestManager;
        }

        public async Task Consume(ConsumeContext<GetHarvestLogInfo> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(GetHarvestLogInfo),
                    context.ConversationId);

                var message = context.Message;
                var result = harvestManager.GetLogInfo(message.Request);
                await context.RespondAsync<GetHarvestLogInfoResult>(new
                {
                    Result = result
                });
            }
        }
    }
}