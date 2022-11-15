using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Asset;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Order.Consumers
{
    public class AssetReadyConsumer : IConsumer<IAssetReady>
    {
        private readonly IOrderDataAccess orderDataAccess;


        public AssetReadyConsumer(IOrderDataAccess orderDataAccess)
        {
            this.orderDataAccess = orderDataAccess;
        }


        public async Task Consume(ConsumeContext<IAssetReady> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command", nameof(IAssetReady));
                var message = context.Message;

                if (message.AssetType == AssetType.Benutzungskopie)
                {
                    GebrauchskopieStatus status;

                    if (message.Valid)
                    {
                        status = GebrauchskopieStatus.ErfolgreichErstellt;
                    }
                    else
                    {
                        status = GebrauchskopieStatus.Fehlgeschlagen;
                    }

                    // Status für aus Benutzungskopie-SIP erstellte Gebrauchskopie setzen
                    await orderDataAccess.UpdateBenutzungskopieStatus(message.OrderItemId, status);
                }
            }
        }
    }
}