using System;
using System.IO;
using System.Threading.Tasks;
using CMI.Contract.Monitoring;
using CMI.Manager.DocumentConverter.Abbyy;
using CMI.Manager.DocumentConverter.Extraction;
using CMI.Manager.DocumentConverter.Properties;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.DocumentConverter.Consumers
{
    public class AbbyyOcrTestConsumer : IConsumer<AbbyyOcrTestRequest>
    {
        private readonly IDocumentManager manager;

        public AbbyyOcrTestConsumer(IDocumentManager manager)
        {
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<AbbyyOcrTestRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(AbbyyOcrTestRequest), context.ConversationId);

                var response = manager.TryOcrTextExtraction(out string text) ? 
                    new AbbyyOcrTestResponse() {Success = true} : 
                    new AbbyyOcrTestResponse() {Success = false, Error = text};

                await context.RespondAsync(response);
            }
        }
    }
}