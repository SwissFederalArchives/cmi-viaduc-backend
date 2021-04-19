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
    public class AbbrechenRequestConsumer : IConsumer<AbbrechenRequest>
    {
        private readonly IPublicOrder manager;
        protected readonly IOrderDataAccess orderDataAccess;
        protected readonly IUserDataAccess userDataAccess;

        public AbbrechenRequestConsumer(IOrderDataAccess orderDataAccess, IPublicOrder manager, IUserDataAccess userDataAccess)
        {
            this.orderDataAccess = orderDataAccess;
            this.manager = manager;
            this.userDataAccess = userDataAccess;
        }

        public async Task Consume(ConsumeContext<AbbrechenRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                foreach (var id in context.Message.OrderItemIds)
                {
                    var item = orderDataAccess.GetOrderItem(id);
                    if (item == null)
                    {
                        throw new Exception("Invalid OrderItem: " + id);
                    }
                }

                var m = context.Message;

                await manager.Abbrechen(m.CurrentUserId, m.OrderItemIds, m.Abbruchgrund, m.BemerkungZumDossier, m.InterneBemerkung);
                await context.RespondAsync(new AbbrechenResponse());
            }
        }
    }
}