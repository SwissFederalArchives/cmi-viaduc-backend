using System.Threading.Tasks;
using CMI.Contract.DocumentConverter;
using CMI.Web.Management.api.Data;
using MassTransit;

namespace CMI.Web.Management.Consumers
{
    public class AbbyyProgressEventConsumer: IConsumer<AbbyyProgressEvent>
    {
        private readonly AbbyyProgressInfo progressInfo;

        public AbbyyProgressEventConsumer(AbbyyProgressInfo progressInfo)
        {
            this.progressInfo = progressInfo;
        }

        public Task Consume(ConsumeContext<AbbyyProgressEvent> context)
        {
            progressInfo.AddOrUpdateProgressInfo(context.Message);
            return Task.CompletedTask;
        }
    }
}