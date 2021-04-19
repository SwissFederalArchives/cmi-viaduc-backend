using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Order.Consumers
{
    public class InVorlageExportierenRequestConsumer : IConsumer<InVorlageExportierenRequest>
    {
        private readonly IPublicOrder manager;
        protected readonly IOrderDataAccess orderDataAccess;
        protected readonly IUserDataAccess userDataAccess;

        public InVorlageExportierenRequestConsumer(IOrderDataAccess orderDataAccess, IPublicOrder manager, IUserDataAccess userDataAccess)
        {
            this.orderDataAccess = orderDataAccess;
            this.manager = manager;
            this.userDataAccess = userDataAccess;
        }

        public async Task Consume(ConsumeContext<InVorlageExportierenRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                string userId = null;
                foreach (var id in context.Message.OrderItemIds)
                {
                    var item = await orderDataAccess.GetOrderItem(id);
                    if (item == null)
                    {
                        throw new Exception("Invalid OrderItem: " + id);
                    }

                    var order = await orderDataAccess.GetOrdering(item.OrderId, false);
                    if (string.IsNullOrEmpty(userId))
                    {
                        userId = order?.UserId;
                    }
                    else
                    {
                        if (userId != order?.UserId)
                            // Der Gesuchsteller wird im Mail angezeigt und mehrere werden nicht unterstützt
                        {
                            throw new BadRequestException("Es dürfen nur Gesuche vom gleichen Gesuchsteller verarbeitet werden");
                        }
                    }
                }

                var m = context.Message;
                await manager.InVorlageExportieren(m.CurrentUserId, m.OrderItemIds, m.Vorlage, m.Sprache);
                await context.RespondAsync(new InVorlageExportierenResponse());
            }
        }
    }
}