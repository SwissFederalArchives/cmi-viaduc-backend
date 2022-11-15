using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.ExternalContent.Consumers
{
    public class DigitizationOrderConsumer : IConsumer<GetDigitizationOrderData>
    {
        private readonly IExternalContentManager externalContentManager;

        public DigitizationOrderConsumer(IExternalContentManager externalContentManager)
        {
            this.externalContentManager = externalContentManager;
        }

        public async Task Consume(ConsumeContext<GetDigitizationOrderData> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(GetDigitizationOrderData),
                    context.ConversationId);

                var message = context.Message;
                var result = externalContentManager.GetDigitizationOrderData(message.ArchiveRecordId);

                Log.Information("Sending {ResponseName} to {ResponseAddress} with conversationId {ConversationId}",
                    nameof(GetDigitizationOrderDataResponse), context.ResponseAddress, context.ConversationId);
                await context.RespondAsync<GetDigitizationOrderDataResponse>(new
                {
                    Result = result
                });
            }
        }
    }
}