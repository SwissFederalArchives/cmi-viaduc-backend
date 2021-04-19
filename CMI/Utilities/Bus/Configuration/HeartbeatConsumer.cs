using System;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Monitoring;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Utilities.Bus.Configuration
{
    public class HeartbeatConsumer : IConsumer<HeartbeatRequest>
    {
        public HeartbeatConsumer(string name)
        {
            Name = name;
        }

        public string Name { get; }

        public async Task Consume(ConsumeContext<HeartbeatRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(HeartbeatRequest),
                    context.ConversationId);

                if (Name == null)
                {
                    Log.Information("HeartbeatRequest without name consumed");
                    return;
                }

                var service = Enum.GetValues(typeof(MonitoredServices)).Cast<MonitoredServices>().First(m => m.ToString() == Name);
                await context.RespondAsync(new HeartbeatResponse(service, HeartbeatStatus.Ok));
                Log.Information("Consumed a Heartbeatrequest of Type {Name}.", Name);
            }
        }
    }
}