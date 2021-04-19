using System.Threading.Tasks;
using CMI.Contract.Common;
using MassTransit;
using Serilog.Context;
using Serilog.Core.Enrichers;

namespace CMI.Manager.Asset.Consumers
{
    public class UpdatePrimaerdatenAuftragStatusConsumer : IConsumer<IUpdatePrimaerdatenAuftragStatus>
    {
        private readonly IAssetManager assetManager;

        public UpdatePrimaerdatenAuftragStatusConsumer(IAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        public Task Consume(ConsumeContext<IUpdatePrimaerdatenAuftragStatus> context)
        {
            var conversationEnricher = new PropertyEnricher(nameof(context.ConversationId), context.ConversationId);

            using (LogContext.Push(conversationEnricher))
            {
                assetManager.UpdatePrimaerdatenAuftragStatus(context.Message);
            }

            return Task.CompletedTask;
        }
    }
}