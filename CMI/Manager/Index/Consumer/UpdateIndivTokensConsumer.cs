using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;

namespace CMI.Manager.Index.Consumer
{
    public class UpdateIndivTokensConsumer : IConsumer<UpdateIndivTokens>
    {
        private readonly IIndexManager indexManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="UpdateArchiveRecordConsumer" /> class.
        /// </summary>
        /// <param name="indexManager">The index manager that is responsible for updating.</param>
        public UpdateIndivTokensConsumer(IIndexManager indexManager)
        {
            this.indexManager = indexManager;
        }

        public Task Consume(ConsumeContext<UpdateIndivTokens> context)
        {
            indexManager.UpdateTokens(context.Message.ArchiveRecordId.ToString(),
                context.Message.CombinedPrimaryDataDownloadAccessTokens,
                context.Message.CombinedPrimaryDataFulltextAccessTokens,
                context.Message.CombinedMetadataAccessTokens,
                context.Message.CombinedFieldAccessTokens);
            return Task.CompletedTask;
        }
    }
}