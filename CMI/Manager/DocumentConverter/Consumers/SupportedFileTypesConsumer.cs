using System.Threading.Tasks;
using CMI.Contract.DocumentConverter;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.DocumentConverter.Consumers
{
    /// <summary>
    ///     Consumes requests for supported file types.
    /// </summary>
    /// <seealso cref="SupportedFileTypesRequest" />
    public class SupportedFileTypesConsumer : IConsumer<SupportedFileTypesRequest>
    {
        private readonly IDocumentManager manager;

        public SupportedFileTypesConsumer(IDocumentManager manager)
        {
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<SupportedFileTypesRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(SupportedFileTypesRequest), context.ConversationId);

                var fileTypes = manager.GetSupportedFileTypes(context.Message.ProcessType);
                await context.RespondAsync(new SupportedFileTypesResponse {SupportedFileTypes = fileTypes});
            }
        }
    }
}