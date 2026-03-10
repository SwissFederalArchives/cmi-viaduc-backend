using System;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using CMI.Manager.DataFeed;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.DataFeed.Consumers
{
    /// <summary>
    ///     Consumer for ActaProSyncRecord messages.
    ///     Stores the received records in SyncAction table for further processing.
    /// </summary>
    public class ActaProSyncRecordConsumer : IConsumer<ActaProSyncRecord>
    {
        private readonly IDataFeedManager dataFeedManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ActaProSyncRecordConsumer"/> class.
        /// </summary>
        /// <param name="dataFeedManager">Manager to process the sync action.</param>
        public ActaProSyncRecordConsumer(IDataFeedManager _dataFeedManager)
        {
            dataFeedManager = _dataFeedManager;
        }

        /// <summary>
        ///     Consumes the ActaProSyncRecord message and stores it for syncing.
        /// </summary>
        /// <param name="context">Message context.</param>
        public async Task Consume(ConsumeContext<ActaProSyncRecord> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                try
                {
                    var message = context.Message;
                    Log.Information("Received {CommandName} message with ArchiveRecordId: {ArchiveRecordId}, Action: {Action}, ConversationId: {ConversationId}",
                        nameof(ActaProSyncRecord), message.ArchiveRecordId, message.Action, context.ConversationId);

                    await dataFeedManager.HandleActaProSyncRecordAsync(message);

                    Log.Information("SyncAction inserted for ArchiveRecordId: {ArchiveRecordId}", message.ArchiveRecordId);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error while handling ActaProSyncRecord message.");
                    throw;
                }
            }
        }
    }
}
