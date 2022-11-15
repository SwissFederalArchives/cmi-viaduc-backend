using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Order.Consumers
{
    public class EntscheidFreigabeHinterlegenRequestConsumer : IConsumer<EntscheidFreigabeHinterlegenRequest>
    {
        private readonly IPublicOrder manager;
        protected readonly IOrderDataAccess orderDataAccess;
        protected readonly IUserDataAccess userDataAccess;

        public EntscheidFreigabeHinterlegenRequestConsumer(IUserDataAccess userDataAccess,
            IOrderDataAccess orderDataAccess, IPublicOrder manager)
        {
            this.manager = manager;
            this.userDataAccess = userDataAccess ?? throw new ArgumentNullException(nameof(userDataAccess));
            this.orderDataAccess = orderDataAccess ?? throw new ArgumentNullException(nameof(orderDataAccess));
        }

        public async Task Consume(ConsumeContext<EntscheidFreigabeHinterlegenRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                foreach (var id in context.Message.OrderItemIds)
                {
                    var item = await orderDataAccess.GetOrderItem(id);
                    if (item == null)
                    {
                        throw new Exception("Invalid OrderItem: " + id);
                    }
                }

                await manager.EntscheidFreigabeHinterlegen(
                    context.Message.UserId,
                    context.Message.OrderItemIds,
                    context.Message.Entscheid,
                    context.Message.DatumBewilligung,
                    context.Message.InterneBemerkung);

                await context.RespondAsync(new EntscheidFreigabeHinterlegenResponse());
            }
        }
    }
}