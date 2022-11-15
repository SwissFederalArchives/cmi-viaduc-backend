using System;
using System.Threading.Tasks;
using CMI.Contract.Harvest;
using CMI.Contract.Monitoring;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Harvest.Consumers
{
    public class CheckAisDbConsumer : IConsumer<AisDbCheckRequest>
    {
        private readonly IDbTestAccess dbTestAccess;

        public CheckAisDbConsumer(IDbTestAccess dbTestAccess)
        {
            this.dbTestAccess = dbTestAccess;
        }

        public async Task Consume(ConsumeContext<AisDbCheckRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(AisDbCheckRequest),
                    context.ConversationId);

                var response = new AisDbCheckResponse();

                try
                {
                    response.DbVersion = dbTestAccess.GetDbVersion();
                    response.Ok = true;
                }
                catch (Exception ex)
                {
                    response.Exception = ex;
                    response.Ok = false;
                }

                await context.RespondAsync(response);
            }
        }
    }
}