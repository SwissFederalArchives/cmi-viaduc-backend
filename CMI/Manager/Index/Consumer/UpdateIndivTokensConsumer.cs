using System;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;

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

        public async Task Consume(ConsumeContext<UpdateIndivTokens> context)
        {
            try
            {
                await indexManager.UpdateTokens(context.Message.ArchiveRecordId,
                    context.Message.CombinedPrimaryDataDownloadAccessTokens,
                    context.Message.CombinedPrimaryDataFulltextAccessTokens,
                    context.Message.CombinedMetadataAccessTokens,
                    context.Message.CombinedFieldAccessTokens);
            }
            catch (Exception e)
            {
                Log.Warning(e, "{CommandName} has an error for record with ArchiveRecordId: {ArchiveRecordId}", nameof(UpdateIndivTokens),
                    context.Message.ArchiveRecordId);
                throw;
            }
          
        }
    }
}