using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMI.Manager.Index.Consumer
{
    internal static class PrimaerdatenAuftragHelper
    {
        public static async Task UpdatePrimaerdatenAuftragStatus(ConsumeContext<IUpdateArchiveRecord> context, 
            AufbereitungsServices service,
            AufbereitungsStatusEnum newStatus, 
            string errorText = null)
        {
            if (context.Message.PrimaerdatenAuftragId > 0)
            {
                Log.Information("Auftrag mit Id {PrimaerdatenAuftragId} wurde im {service}-Service auf Status {Status} gesetzt.",
                    context.Message.PrimaerdatenAuftragId, service.ToString(), newStatus.ToString());

                var ep = await context.GetSendEndpoint(new Uri(context.SourceAddress,
                    BusConstants.AssetManagerUpdatePrimaerdatenAuftragStatusMessageQueue));
                await ep.Send<IUpdatePrimaerdatenAuftragStatus>(new UpdatePrimaerdatenAuftragStatus
                {
                    PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                    Service = service,
                    Status = newStatus,
                    ErrorText = errorText
                });
            }
        }
    }
}
