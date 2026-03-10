using System;
using System.IO;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using CMI.Utilities.ActaPro;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Cache
{
    public class DoesExistInCacheRequestConsumer : IConsumer<DoesExistInCacheRequest>
    {
        public DoesExistInCacheRequestConsumer()
        {
            DoesExistFunc = context =>
            {
                var path = Path.Combine(
                    Properties.CacheSettings.Default.BaseDirectory,
                    context.Message.RetentionCategory.ToString(),
                    context.Message.Id);

                var exists = false;
                long fileSizeInBytes = 0;
                try
                {
                    var file = new FileInfo(path);
                    if (file.Exists)
                    {
                        exists = true;
                        fileSizeInBytes = file.Length;
                    }
                    else
                    {
                        // Try alternative path
                        var mappingProvider = new ActaProMappingProvider();
                        var otherId = mappingProvider.GetOtherId(context.Message.Id);

                        if (otherId != "-1")
                        {
                            path = Path.Combine(
                                Properties.CacheSettings.Default.BaseDirectory,
                                context.Message.RetentionCategory.ToString(),
                                otherId);

                            file = new FileInfo(path);
                            if (file.Exists)
                            {
                                exists = true;
                                fileSizeInBytes = file.Length;

                                // In this case rename the file to the expected id
                                var newPath = Path.Combine(
                                    Properties.CacheSettings.Default.BaseDirectory,
                                    context.Message.RetentionCategory.ToString(),
                                    context.Message.Id);
                                file.MoveTo(newPath);
                                Log.Information("Renamed cached file from {oldPath} to {newPath}", path, newPath);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Error while checking if file {FILE} is in Cache", path);
                }

                return new Tuple<bool, long>(exists, fileSizeInBytes);
            };
        }

        public Func<ConsumeContext<DoesExistInCacheRequest>, Tuple<bool, long>> DoesExistFunc { get; set; }

        public async Task Consume(ConsumeContext<DoesExistInCacheRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(DoesExistInCacheRequest),
                    context.ConversationId);

                // With newer .NET Framework the deconstruction of the result into two variables does not work anymore.
                // Need to assign to variable and access the items individually.
                var funcResult = DoesExistFunc(context);

                Log.Information("FileExists: {exists}", funcResult.Item1);
                await context.RespondAsync(new DoesExistInCacheResponse
                {
                    Exists = funcResult.Item1,
                    FileSizeInBytes = funcResult.Item2
                });

                Log.Information("Response sent.");
            }
        }
    }
}