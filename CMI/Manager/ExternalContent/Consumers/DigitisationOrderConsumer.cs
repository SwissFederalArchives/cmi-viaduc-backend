using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Newtonsoft.Json;
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
                var result = await externalContentManager.GetDigitizationOrderData(message.ArchiveRecordId);

                await context.RespondAsync<GetDigitizationOrderDataResponse>(new
                {
                    Result = result
                });

                if (result.Success && result.DigitizationOrder != null)
                {
                    try
                    {
                        Log.Information("Sending {ResponseName} to {ResponseAddress} with conversationId {ConversationId}",
                            nameof(GetDigitizationOrderDataResponse), context.ResponseAddress, context.ConversationId);
                        Log.Information("Result is: {serializedValue}", result.DigitizationOrder.Serialize());
                    }
                    catch (System.Exception ex)
                    {
                        Log.Error(ex, "Unexpected error while writing result to log after getting digitizationOrder");
                        throw;
                    }
                }

            }
        }
    }
}