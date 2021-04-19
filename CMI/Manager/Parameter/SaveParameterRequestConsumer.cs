using System.Threading.Tasks;
using CMI.Contract.Parameter.SaveParameter;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Parameter
{
    public class SaveParameterRequestConsumer : IConsumer<SaveParameterRequest>
    {
        public async Task Consume(ConsumeContext<SaveParameterRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(SaveParameterRequest), context.ConversationId);

                ParameterRequestResponseHelper.Instance.ClearErrorMessages();
                await ParameterService.ParameterBus.Publish(new SaveParameterEvent(context.Message.Parameter));

                Log.Verbose("Save Event started");
                await Task.Delay(400);

                await context.RespondAsync(new SaveParameterResponse
                {
                    ErrorMessages = ParameterRequestResponseHelper.Instance.GetErrorMessages()
                });

                Log.Verbose("Save Event response sent");
            }
        }
    }
}