using System;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Order.Consumers
{
    public class FindOrderHistoryForVeConsumer : IConsumer<FindOrderingHistoryForVeRequest>
    {
        protected readonly IPublicOrder publicOrder;

        public FindOrderHistoryForVeConsumer(IPublicOrder publicOrder)
        {
            this.publicOrder = publicOrder ?? throw new ArgumentNullException(nameof(publicOrder));
        }

        public async Task Consume(ConsumeContext<FindOrderingHistoryForVeRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                var historyEntries = await publicOrder.GetOrderingHistoryForVe(context.Message.VeId);
                var response = new FindOrderingHistoryForVeResponse {History = historyEntries};

                await context.RespondAsync(response);
            }
        }
    }
}