using System.Threading.Tasks;
using CMI.Contract.Order;
using MassTransit;
using Newtonsoft.Json;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Order.Consumers
{
    public class ErinnerungVersendenRequestConsumer : IConsumer<ErinnerungVersendenRequest>
    {
        private readonly IPublicOrder manager;

        public ErinnerungVersendenRequestConsumer(IPublicOrder manager)
        {
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<ErinnerungVersendenRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                var msg = context.Message;

                var result = await manager.ErinnerungVersenden(msg.OrderItemIds, msg.UserId);
                await context.RespondAsync(result);

                Log.Information("Response sent for {CommandName} command with conversationId {ConversationId}. Result is {result}",
                    context.Message.GetType().Name, context.ConversationId, JsonConvert.SerializeObject(result));
            }
        }
    }
}