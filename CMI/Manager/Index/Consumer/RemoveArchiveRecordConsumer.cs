using System;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Index.Consumer
{
    /// <summary>
    ///     Class RemoveArchiveRecordConsumer.
    /// </summary>
    /// <seealso cref="IRemoveArchiveRecord" />
    public class RemoveArchiveRecordConsumer : IConsumer<IRemoveArchiveRecord>
    {
        private readonly IIndexManager indexManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RemoveArchiveRecordConsumer" /> class.
        /// </summary>
        /// <param name="indexManager">The index manager that is responsible for removal.</param>
        public RemoveArchiveRecordConsumer(IIndexManager indexManager)
        {
            this.indexManager = indexManager;
        }

        /// <summary>
        ///     Consumes the specified message from the bus
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Task.</returns>
        public async Task Consume(ConsumeContext<IRemoveArchiveRecord> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(IRemoveArchiveRecord),
                    context.ConversationId);

                try
                {
                    indexManager.RemoveArchiveRecord(context);

                    await context.Publish<IArchiveRecordRemoved>(new
                    {
                        context.Message.MutationId,
                        ActionSuccessful = true
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to remove archiveRecord with conversationId {ConversationId} in Elastic or SQL", context.ConversationId);
                    await context.Publish<IArchiveRecordRemoved>(new
                    {
                        context.Message.MutationId,
                        ActionSuccessful = false,
                        ErrorMessage = ex.Message,
                        ex.StackTrace
                    });
                }
            }
        }
    }
}