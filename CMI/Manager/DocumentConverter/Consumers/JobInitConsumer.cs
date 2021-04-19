using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.DocumentConverter;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.DocumentConverter.Consumers
{
    internal class JobInitConsumer : IConsumer<JobInitRequest>
    {
        private readonly IDocumentManager manager;
        private readonly SftpServer sftpServer;

        public JobInitConsumer(SftpServer sftpServer, IDocumentManager manager)
        {
            this.sftpServer = sftpServer;
            this.manager = manager;
        }

        public Task Consume(ConsumeContext<JobInitRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(JobInitRequest), context.ConversationId);

                // Verify if file type is supported 
                var supportedFileTypes = manager.GetSupportedFileTypes(context.Message.RequestedProcessType);
                var fileToConvert = new FileInfo(context.Message.FileNameWithExtension);

                if (supportedFileTypes.Contains(fileToConvert.Extension.Substring(1).ToLower()))
                {
                    var jobInitResult = sftpServer.RegisterNewJob(context.Message);
                    return context.RespondAsync(jobInitResult);
                }

                var errorResult = new JobInitResult
                {
                    IsInvalid = true,
                    ErrorMessage =
                        $"Extension {fileToConvert.Extension} is not supported. Supported Extensions are {string.Join(", ", supportedFileTypes)}"
                };

                return context.RespondAsync(errorResult);
            }
        }
    }
}