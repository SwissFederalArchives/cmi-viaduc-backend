using CMI.Contract.Messaging;
using CMI.Engine.Asset.PostProcess;
using MassTransit;
using Serilog;
using System.Threading.Tasks;
using CMI.Engine.Asset;

namespace CMI.Manager.Asset.Consumers
{
    public class DeleteViewerFilesConsumer : IConsumer<IDeleteViewerFiles>
    {
        private readonly IManifestHelper manifestHelper;

        public DeleteViewerFilesConsumer(IManifestHelper manifestHelper)
        {
            this.manifestHelper = manifestHelper;
        }

        public Task Consume(ConsumeContext<IDeleteViewerFiles> context)
        {
            Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
             nameof(DeleteViewerFilesConsumer), context.ConversationId);

            manifestHelper.DeleteIiifFiles(context.Message.ManifestLink);
            return Task.CompletedTask;
        }
    }
}
