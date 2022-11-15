using System.Threading.Tasks;
using CMI.Contract.Parameter.GetParameter;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Parameter
{
    public class GetParameterRequestConsumer : IConsumer<GetParameterRequest>
    {
        public async Task Consume(ConsumeContext<GetParameterRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(GetParameterRequest), context.ConversationId);

                await context.RespondAsync(new GetParameterResponse
                {
                    Parameters = await ParameterRequestResponseHelper.Instance.GetParameters()
                });

                Log.Information("response sent");
            }
        }
    }
}