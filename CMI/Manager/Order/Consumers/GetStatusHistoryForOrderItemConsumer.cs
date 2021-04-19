using System;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Order.Consumers
{
    public class GetStatusHistoryForOrderItemConsumer : IConsumer<GetStatusHistoryForOrderItemRequest>
    {
        protected readonly IPublicOrder publicOrder;

        public GetStatusHistoryForOrderItemConsumer(IPublicOrder publicOrder)
        {
            this.publicOrder = publicOrder ?? throw new ArgumentNullException(nameof(publicOrder));
        }

        public async Task Consume(ConsumeContext<GetStatusHistoryForOrderItemRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                var historyEntries = await publicOrder.GetStatusHistoryForOrderItem(context.Message.OrderItemId);
                var response = new GetStatusHistoryForOrderItemResponse {StatusHistory = historyEntries};

                await context.RespondAsync(response);
            }
        }
    }
}