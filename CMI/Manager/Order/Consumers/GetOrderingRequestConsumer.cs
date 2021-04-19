using System.Threading.Tasks;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Order.Consumers
{
    public class GetOrderingRequestConsumer : IConsumer<GetOrderingRequest>
    {
        private readonly IPublicOrder manager;

        public GetOrderingRequestConsumer(IPublicOrder manager)
        {
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<GetOrderingRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                var ordering = await manager.GetOrdering(context.Message.OrderingId);
                var response = new GetOrderingResponse {Ordering = ordering};

                await context.RespondAsync(response);
            }
        }
    }
}