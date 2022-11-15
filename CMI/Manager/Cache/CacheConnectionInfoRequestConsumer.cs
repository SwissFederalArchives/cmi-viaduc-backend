using System;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Cache
{
    public class CacheConnectionInfoRequestConsumer : IConsumer<CacheConnectionInfoRequest>
    {
        public async Task Consume(ConsumeContext<CacheConnectionInfoRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(CacheConnectionInfoRequest), context.ConversationId);

                await context.RespondAsync(new CacheConnectionInfoResponse
                {
                    Port = ((long?) Properties.CacheSettings.Default.Port).Value,
                    Machine = Properties.CacheSettings.Default.BaseAddress.Replace("{MachineName}", Environment.MachineName),
                    Password = Password.Current
                });

                Log.Information("Response sent.");
            }
        }
    }
}