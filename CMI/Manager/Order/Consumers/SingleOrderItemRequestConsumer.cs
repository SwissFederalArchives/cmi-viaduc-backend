using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Order.Consumers
{
    /// <summary>
    ///     Abstrakte Basisklasse für Consumers, welche EINE EINZIGE Order pro Request verarbeiten
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    public abstract class SingleOrderItemRequestConsumer<TRequest, TResponse> : IConsumer<TRequest>
        where TRequest : class, ISingleOrderId where TResponse : class
    {
        protected readonly IOrderDataAccess dataAccess;
        protected readonly StatusWechsler statuswechsler;

        protected SingleOrderItemRequestConsumer(IOrderDataAccess dataAccess, StatusWechsler statuswechsler)
        {
            this.dataAccess = dataAccess ?? throw new ArgumentNullException(nameof(dataAccess));
            this.statuswechsler = statuswechsler ?? throw new ArgumentNullException(nameof(statuswechsler));
        }

        public async Task Consume(ConsumeContext<TRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    context.Message.GetType().Name, context.ConversationId);

                var item = await dataAccess.GetOrderItem(context.Message.OrderItemId);

                if (item == null)
                {
                    throw new Exception("Invalid OrderItem: " + context.Message.OrderItemId);
                }

                var response = await CreateResponse(item, context.Message);
                await context.RespondAsync(response);
            }
        }

        public abstract Task<TResponse> CreateResponse(OrderItem orderItem, TRequest Request);
    }
}