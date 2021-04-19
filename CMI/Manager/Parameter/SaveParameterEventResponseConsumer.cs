using System.Threading.Tasks;
using CMI.Contract.Parameter.SaveParameter;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Parameter
{
    public class SaveParameterEventResponseConsumer : IConsumer<SaveParameterEventResponse>
    {
        public Task Consume(ConsumeContext<SaveParameterEventResponse> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(SaveParameterEventResponse), context.ConversationId);

                if (context.Message.ErrorMessage == string.Empty)
                {
                    ParameterRequestResponseHelper.Instance.AppendErrorMessage(string.Empty);
                    Log.Information("Saved Successfully");
                }
                else if (context.Message.ErrorMessage != "Setting was not found")
                {
                    ParameterRequestResponseHelper.Instance.AppendErrorMessage(context.Message.ErrorMessage);
                    Log.Warning("An Error has occurred while saving!");
                }
            }

            return Task.CompletedTask;
        }
    }
}