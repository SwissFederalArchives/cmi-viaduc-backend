using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.DocumentConverter;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.DocumentConverter.Consumers
{
    internal class JobEndConsumer : IConsumer<JobEndRequest>
    {
        private readonly SftpServer sftpServer;

        public JobEndConsumer(SftpServer sftpServer)
        {
            this.sftpServer = sftpServer;
        }

        public Task Consume(ConsumeContext<JobEndRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(JobEndRequest), context.ConversationId);

                try
                {
                    sftpServer.RemoveJob(context.Message.JobGuid);
                    return context.RespondAsync(new JobEndResult());
                }
                catch (System.Exception ex)
                {
                    Log.Error(ex, $"Unexpected error while removing job with id {context.Message.JobGuid}");
                    var errorResult = new JobEndResult
                    {
                        IsInvalid = true,
                        ErrorMessage =
                            $"Could not remove job {context.Message.JobGuid}."
                    };
                    return context.RespondAsync(errorResult);
                }
            }
        }
    }
}