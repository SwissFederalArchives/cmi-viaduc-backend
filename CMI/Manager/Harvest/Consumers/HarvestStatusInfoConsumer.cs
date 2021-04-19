using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Harvest.Consumers
{
    public class HarvestStatusInfoConsumer : IConsumer<GetHarvestStatusInfo>
    {
        private readonly IHarvestManager harvestManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="HarvestStatusInfoConsumer" /> class.
        /// </summary>
        /// <param name="harvestManager">The harvest manager.</param>
        public HarvestStatusInfoConsumer(IHarvestManager harvestManager)
        {
            this.harvestManager = harvestManager;
        }

        public async Task Consume(ConsumeContext<GetHarvestStatusInfo> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(GetHarvestStatusInfo),
                    context.ConversationId);

                var message = context.Message;
                var result = harvestManager.GetStatusInfo(message.DateRange);
                await context.RespondAsync<GetHarvestStatusInfoResult>(new
                {
                    Result = result
                });
            }
        }
    }
}