using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Utilities.Cache.Access;
using MassTransit;
using Newtonsoft.Json;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Order.Consumers
{
    public class DigitalisierungsAuftragErledigtConsumer : IConsumer<IDigitalisierungsAuftragErledigt>
    {
        private readonly IBus bus;
        private readonly IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse> findArchiveRecordClient;
        private readonly IOrderDataAccess orderDataAccess;
        private readonly ICacheHelper cacheHelper;


        public DigitalisierungsAuftragErledigtConsumer(IBus bus,
            IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse> findArchiveRecordClient, IOrderDataAccess orderDataAccess, ICacheHelper cacheHelper)
        {
            this.bus = bus;
            this.findArchiveRecordClient = findArchiveRecordClient;
            this.orderDataAccess = orderDataAccess;
            this.cacheHelper = cacheHelper;
        }


        public async Task Consume(ConsumeContext<IDigitalisierungsAuftragErledigt> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                var message = context.Message;
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", message.GetType().Name,
                    context.ConversationId);

                // Wir warten hier, damit wir sicher sind, das Elastic den neuen Record auch aktualisiert und nach aussen öffentlich verfügbar hat.
                // Nicht vergessen. Elastic ist nur "near real time". https://www.elastic.co/guide/en/elasticsearch/reference/master/near-real-time.html 
                await Task.Delay(3000);
                var archiveRecord = await findArchiveRecordClient.Request(new FindArchiveRecordRequest {ArchiveRecordId = message.ArchiveRecordId});

                // Ist das LastSyncDate der VE im Elastic-Index grösser als das Bestelldatum? (--> Es wurde eine Synchronisation mit dem AIS durchgeführt.)
                // und enthält das Attribut PrimaryDataLink der VE im Elastic-Index einen Wert? (--> Es sind Primärdaten vorhanden.)
                // dann sind die Primärdaten im DIR hinterlegt worden und die Daten sind bereit.
                if (archiveRecord.ElasticArchiveRecord.LastSyncDate > message.OrderDate &&
                    !string.IsNullOrEmpty(archiveRecord.ElasticArchiveRecord.PrimaryDataLink))
                {
                    // Die Daten sind vorhanden. Durch auslösen des Downloads werden die Daten aufbereitet und der Benutzer informiert.
                    var ep = await context.GetSendEndpoint(new Uri(bus.Address, BusConstants.RepositoryManagerDownloadPackageMessageQueue));
                    await ep.Send<IDownloadPackage>(new DownloadPackage
                    {
                        PackageId = archiveRecord.ElasticArchiveRecord.PrimaryDataLink,
                        CallerId = message.GetType().Name,
                        ArchiveRecordId = message.ArchiveRecordId,
                        RetentionCategory = await cacheHelper.GetRetentionCategory(archiveRecord.ElasticArchiveRecord,
                            message.OrderUserRolePublicClient, orderDataAccess),
                        Recipient = message.OrderUserId
                    });
                }
                else
                {
                    Log.Information(
                        "Die VE mit Id {ArchiveRecordId} wurde digitalisiert, aber bis jetzt nicht neu synchronisiert. Wir schreiben sie in die Wait List",
                        message.ArchiveRecordId);
                    await orderDataAccess.AddToOrderExecutedWaitList(Convert.ToInt32(message.ArchiveRecordId), JsonConvert.SerializeObject(message));
                }
            }
        }
    }

    public class PrimaryDataNotReadyException : Exception
    {
        public PrimaryDataNotReadyException(string message) : base(message)
        {
        }
    }
}