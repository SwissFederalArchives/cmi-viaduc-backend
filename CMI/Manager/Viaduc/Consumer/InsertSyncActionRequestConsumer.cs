using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc.EF;
using LogContext = Serilog.Context.LogContext;


namespace CMI.Manager.Viaduc.Consumer
{
    public class InsertSyncActionRequestConsumer : IConsumer<InsertSyncActionRequest>
    {
        private readonly IViaducDataProvider dataProvider;

        public InsertSyncActionRequestConsumer(IViaducDataProvider dataProvider)
        {
            this.dataProvider = dataProvider;
        }

        /// <summary>
        ///     Consumes the specified message from the bus
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Task.</returns>
        public async Task Consume(ConsumeContext<InsertSyncActionRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(InsertSyncActionRequest),
                    context.ConversationId);

                try
                {
                    await dataProvider.InsertSyncAction(context.Message.SyncActionDto);
                    await context.RespondAsync(new InsertSyncActionResponse());
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to insert syncAction for archiveRecord {archiveRecordId} with conversationId {ConversationId} into SQL database", context.Message.SyncActionDto.ArchiveRecordId, context.ConversationId);
                }
            }
        }
    }
}
