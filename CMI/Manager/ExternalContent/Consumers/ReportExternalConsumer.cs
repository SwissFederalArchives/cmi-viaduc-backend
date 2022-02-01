using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.ExternalContent.Consumers
{
    public class ReportExternalConsumer : IConsumer<SyncInfoForReportRequest>
    {
        private readonly IReportExternalContentManager reportExternalContentManager;

        public ReportExternalConsumer(IReportExternalContentManager reportExternalContentManager)
        {
            this.reportExternalContentManager = reportExternalContentManager;
        }

        public async Task Consume(ConsumeContext<SyncInfoForReportRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(SyncInfoForReportRequest),
                    context.ConversationId);

                var message = context.Message;
                var result = reportExternalContentManager.GetReportExternalContent(message.MutationsIds);

                Log.Information("Sending {ResponseName} to {ResponseAddress} with conversationId {ConversationId}",
                    nameof(SyncInfoForReportResponse), context.ResponseAddress, context.ConversationId);
                await context.RespondAsync<SyncInfoForReportResponse>(new
                {
                    Result = result
                });
            }
        }
    }
}
