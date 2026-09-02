using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Index.Consumer
{
    public class FindAllArchiveRecordsFromContainerConsumer : IConsumer<FindAllArchiveRecordsFromContainerRequest>
    {
        private readonly IIndexManager indexManager;

        public FindAllArchiveRecordsFromContainerConsumer(IIndexManager indexManager)
        {
            this.indexManager = indexManager;
        }

        public async Task Consume(ConsumeContext<FindAllArchiveRecordsFromContainerRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(FindAllArchiveRecordsFromContainerRequest),
                    context.ConversationId);

                try
                {
                    await context.RespondAsync(new FindAllArchiveRecordsFromContainerResponse
                    {
                       ElasticArchiveRecords  = await indexManager.FindDocument(new Dictionary<string, string>
                       {
                           {"containers.containerCode", context.Message.ContainerCode}
                       },
                           // lese alle Einträge aus
                           10000, context.Message.UseUnanonymizedData)
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
