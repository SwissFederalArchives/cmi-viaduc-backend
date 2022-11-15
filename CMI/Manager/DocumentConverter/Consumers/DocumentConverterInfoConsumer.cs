using System.Threading.Tasks;
using CMI.Contract.Monitoring;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.DocumentConverter.Consumers
{
    internal class DocumentConverterInfoConsumer : IConsumer<DocumentConverterInfoRequest>
    {
        private readonly IDocumentManager manager;

        public DocumentConverterInfoConsumer(IDocumentManager manager)
        {
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<DocumentConverterInfoRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(DocumentConverterInfoRequest), context.ConversationId);

                var response = new DocumentConverterInfoResponse {PagesRemaining = manager.GetPagesRemaining()};

                await context.RespondAsync(response);
            }
        }
    }
}