using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using System;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc.EF;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Viaduc.Consumer
{
    public class DeleteOldSyncActionRequestConsumer(IViaducDataProvider dataProvider) : IConsumer<DeleteOldSyncActionRequest>
    {
        /// <summary>
        ///     Consumes the specified message from the bus
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>Task.</returns>
        public async Task Consume(ConsumeContext<DeleteOldSyncActionRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(DeleteOldSyncActionRequest),
                    context.ConversationId);

                try
                {
                    await dataProvider.DeleteOldSyncActionAsync(context.Message.DaysAgo);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to delete old syncAction with conversationId {ConversationId} into SQL database", context.ConversationId);
                }
            }
        }
    }
}
