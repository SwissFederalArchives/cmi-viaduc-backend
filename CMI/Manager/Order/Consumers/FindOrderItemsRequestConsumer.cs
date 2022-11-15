using System.Threading.Tasks;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Order.Consumers
{
    public class FindOrderItemsRequestConsumer : IConsumer<FindOrderItemsRequest>
    {
        private readonly IPublicOrder manager;

        public FindOrderItemsRequestConsumer(IPublicOrder manager)
        {
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<FindOrderItemsRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                var orderItems = await manager.FindOrderItems(context.Message.OrderItemIds);

                var response = new FindOrderItemsResponse {OrderItems = orderItems};

                await context.RespondAsync(response);
            }
        }
    }
}