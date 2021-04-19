using System.Threading.Tasks;
using CMI.Contract.Parameter.GetParameter;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Parameter
{
    public class GetParameterEventResponseConsumer : IConsumer<GetParameterEventResponse>
    {
        public Task Consume(ConsumeContext<GetParameterEventResponse> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(GetParameterEventResponse), context.ConversationId);

                if (context?.Message?.Parameters?.Length > 0)
                {
                    ParameterRequestResponseHelper.Instance.AppendParam(context.Message.Parameters);
                    Log.Verbose("Appended " + context.Message.Parameters.Length + ":");

                    foreach (var x in context.Message.Parameters)
                    {
                        Log.Verbose("- " + x.Name);
                    }
                }

                Log.Verbose("Parameter Received return");
            }

            return Task.CompletedTask;
        }
    }
}