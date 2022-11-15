using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Newtonsoft.Json;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Order.Consumers
{
    /// <summary>
    ///     Fetches IArchiveRecordUpdated commands from the bus
    /// </summary>
    /// <seealso cref="IArchiveRecordUpdated" />
    public class ArchiveRecordUpdatedConsumer : IConsumer<IArchiveRecordUpdated>
    {
        private readonly IBus bus;
        private readonly IOrderDataAccess orderDataAccess;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ArchiveRecordUpdatedConsumer" /> class.
        /// </summary>
        public ArchiveRecordUpdatedConsumer(IBus bus, IOrderDataAccess orderDataAccess)
        {
            this.bus = bus;
            this.orderDataAccess = orderDataAccess;
        }

        /// <summary>
        ///     Consumes the specified message from the bus.
        /// </summary>
        /// <param name="context">The context from the bus.</param>
        /// <returns>Task.</returns>
        public async Task Consume(ConsumeContext<IArchiveRecordUpdated> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} event with conversationId {ConversationId} from the bus", nameof(IArchiveRecordUpdated),
                    context.ConversationId);

                // Nur wenn die VE erfolgreich aktualisiert worden ist, prüfen wir, ob es zur VE Einträge in der WaitList hat.
                // In der Regel sollte nur ein Wert zurück geliefert werden.-
                // Wenn ja, dann rufen wir erneut die DigitalisierungsAuftragErledigtEvent auf. Dieser löst dann den Download aus.
                if (context.Message.ActionSuccessful)
                {
                    var items = await orderDataAccess.GetVeFromOrderExecutedWaitList(context.Message.ArchiveRecordId);
                    foreach (var item in items)
                    {
                        var message = JsonConvert.DeserializeObject<DigitalisierungsAuftragErledigt>(item.SerializedMessage);
                        var ep = await context.GetSendEndpoint(new Uri(bus.Address, BusConstants.DigitalisierungsAuftragErledigtEvent));
                        await ep.Send<IDigitalisierungsAuftragErledigt>(new
                        {
                            message.ArchiveRecordId,
                            message.OrderItemId,
                            message.OrderDate,
                            message.OrderUserId,
                            message.OrderUserRolePublicClient
                        });
                        Log.Information("Sent {IDigitalisierungsAuftragErledigt} message on bus from ArchiveRecordUpdatedConsumer",
                            nameof(IDigitalisierungsAuftragErledigt));

                        await orderDataAccess.MarkOrderAsProcessedInWaitList(item.OrderExecutedWaitListId);
                    }
                }
            }
        }
    }
}