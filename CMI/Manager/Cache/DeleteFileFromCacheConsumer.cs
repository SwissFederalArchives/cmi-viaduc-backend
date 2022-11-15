using System;
using System.IO;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Cache
{
    public class DeleteFileFromCacheConsumer : IConsumer<IDeleteFileFromCache>
    {
        public Task Consume(ConsumeContext<IDeleteFileFromCache> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(IDeleteFileFromCache),
                    context.ConversationId);

                // We do not really know or care in which cache subdirectory the file is stored
                // Therefore we check each subdirectory and delete the file if it exists.
                var archiveRecordId = context.Message.ArchiveRecordId;
                var retentionCategories = Enum.GetNames(typeof(CacheRetentionCategory));
                foreach (var retentionCategory in retentionCategories)
                {
                    try
                    {
                        var file = Path.Combine(Properties.CacheSettings.Default.BaseDirectory, retentionCategory, archiveRecordId);
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                            Log.Information("Successfully delete file {archiveRecordId} from cache", archiveRecordId);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unexpexted error while deleting file {archiveRecordId} from cache", archiveRecordId);
                        throw;
                    }
                }

                Log.Information("Removed file {archiveRecordId} from cache.", context.Message.ArchiveRecordId);
            }

            return Task.CompletedTask;
        }
    }
}