using System.Threading.Tasks;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Order.Consumers
{
    public class DigitalisierungAusloesenRequestConsumer : IConsumer<DigitalisierungAusloesenRequest>
    {
        private readonly IPublicOrder manager;

        public DigitalisierungAusloesenRequestConsumer(IPublicOrder manager)
        {
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<DigitalisierungAusloesenRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                await manager.DigitalisierungAusloesen(context.Message.CurrentUserId, context.Message.Snapshots, context.Message.ArtDerArbeit);
                await context.RespondAsync(new DigitalisierungAusloesenResponse());
            }
        }
    }
}