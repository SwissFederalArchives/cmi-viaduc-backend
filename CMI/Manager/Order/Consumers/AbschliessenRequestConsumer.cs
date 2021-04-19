using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Order.Consumers
{
    public class AbschliessenRequestConsumer : IConsumer<AbschliessenRequest>
    {
        protected readonly IPublicOrder manager;
        protected readonly IOrderDataAccess orderDataAccess;
        protected readonly IUserDataAccess userDataAccess;

        public AbschliessenRequestConsumer(IOrderDataAccess orderDataAccess, IPublicOrder manager, IUserDataAccess userDataAccess)
        {
            this.orderDataAccess = orderDataAccess;
            this.manager = manager;
            this.userDataAccess = userDataAccess;
        }

        public async Task Consume(ConsumeContext<AbschliessenRequest> context)
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

                await manager.Abschliessen(context.Message.CurrentUserId, context.Message.OrderItemIds);
                await context.RespondAsync(new AbschliessenResponse());
            }
        }
    }
}