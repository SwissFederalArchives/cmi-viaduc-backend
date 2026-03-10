using CMI.Contract.Messaging;
using MassTransit;
using Quartz;
using Serilog;
using System.Threading.Tasks;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.DataFeed.Consumers;

public class SchedulerTriggerConsumer : IConsumer<ISchedulerTrigger>
{
    private readonly IScheduler scheduler;

    public SchedulerTriggerConsumer(IScheduler scheduler)
    {
        this.scheduler = scheduler;
    }

    public async Task Consume(ConsumeContext<ISchedulerTrigger> context)
    {
        using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
        {
            Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(ISchedulerTrigger),
                context.ConversationId);
            await scheduler.TriggerJob(new JobKey("checkMutationQueueJob", "standardGroup"));
        }
    }
}