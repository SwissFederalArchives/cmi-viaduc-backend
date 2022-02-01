using System.Threading.Tasks;
using CMI.Contract.DocumentConverter;
using CMI.Web.Management.api.Data;
using MassTransit;
using Serilog;

namespace CMI.Web.Management.Consumers
{
    public class DocumentConverterServiceStartedEventConsumer : IConsumer<DocumentConverterServiceStartedEvent>
    {
        private readonly AbbyyProgressInfo progressInfo;

        public DocumentConverterServiceStartedEventConsumer(AbbyyProgressInfo progressInfo)
        {
            this.progressInfo = progressInfo;
        }

        public Task Consume(ConsumeContext<DocumentConverterServiceStartedEvent> context)
        {
            // On startup of the document converter service remove all progress info 
            // that may be around due to a crash or restart of the document converter service.
            Log.Information("Received event that DocumentConverter service was started at {startTime}.", context.Message.StartTime);
            progressInfo.RemoveAll();
            return Task.CompletedTask;
        }
    }
}