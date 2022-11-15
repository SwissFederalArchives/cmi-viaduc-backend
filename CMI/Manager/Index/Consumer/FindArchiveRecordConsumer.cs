using System;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Index.Consumer
{
    public class FindArchiveRecordConsumer : IConsumer<FindArchiveRecordRequest>
    {
        private readonly IIndexManager indexManager;

        public FindArchiveRecordConsumer(IIndexManager indexManager)
        {
            this.indexManager = indexManager;
        }

        public async Task Consume(ConsumeContext<FindArchiveRecordRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(FindArchiveRecordRequest),
                    context.ConversationId);

                try
                {
                    await context.RespondAsync(new FindArchiveRecordResponse
                    {
                        ArchiveRecordId = context.Message.ArchiveRecordId,
                        ElasticArchiveRecord = indexManager.FindArchiveRecord(
                            context.Message.ArchiveRecordId, 
                            context.Message.IncludeFulltextContent, 
                            context.Message.UseUnanonymizedData)
                    });
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to read archiveRecord with conversationId {ConversationId} in Elastic", context.ConversationId);
                    throw;
                }
            }
        }
    }
}