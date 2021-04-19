using System.Threading.Tasks;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Order.Consumers
{
    public class UpdateOrderDetailRequestConsumer : IConsumer<UpdateOrderDetailRequest>
    {
        private readonly IPublicOrder manager;

        public UpdateOrderDetailRequestConsumer(IPublicOrder manager)
        {
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<UpdateOrderDetailRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                await manager.UpdateOrderDetail(context.Message.UpdateData);

                var response = new UpdateOrderDetailResponse();
                await context.RespondAsync(response);

                Log.Information("Response sent");
            }
        }
    }
}