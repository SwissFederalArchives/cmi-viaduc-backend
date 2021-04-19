using System.Threading.Tasks;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Order.Consumers
{
    public class CreateOrderFromBasketRequestConsumer : IConsumer<OrderCreationRequest>
    {
        protected readonly IPublicOrder manager;

        public CreateOrderFromBasketRequestConsumer(IPublicOrder manager)
        {
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<OrderCreationRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                var m = context.Message;

                await manager.CreateOrderFromBasket(m);
                await context.RespondAsync(new CreateOrderFromBasketResponse());
            }
        }
    }
}