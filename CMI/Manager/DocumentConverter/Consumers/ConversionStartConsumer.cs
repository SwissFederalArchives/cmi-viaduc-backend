using System;
using System.IO;
using System.Threading.Tasks;
using CMI.Contract.DocumentConverter;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.DocumentConverter.Consumers
{
    internal class ConversionStartConsumer : IConsumer<ConversionStartRequest>
    {
        private readonly IDocumentManager manager;
        private readonly SftpServer sftpServer;


        public ConversionStartConsumer(SftpServer sftpServer, IDocumentManager manager)
        {
            this.sftpServer = sftpServer;
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<ConversionStartRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                FileInfo sourceFile = null;

                try
                {
                    Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                        nameof(ConversionStartRequest), context.ConversationId);

                    var jobGuid = context.Message.JobGuid;

                    var jobInfo = sftpServer.GetJobInfo(jobGuid);

                    if (jobInfo == null)
                    {
                        Log.Error("Could not retrieve information about job with id {jobGuid}", jobGuid);
                        throw new InvalidOperationException($"Could not retrieve information about job with id {jobGuid}");
                    }

                    // Get source file
                    sourceFile = new FileInfo(Path.Combine(sftpServer.GetJobDirectory(jobGuid).FullName,
                        new FileInfo(jobInfo.Request.FileNameWithExtension).Name));

                    if (!sourceFile.Exists)
                    {
                        throw new FileNotFoundException(sourceFile.FullName);
                    }

                    Log.Information("Starting conversion for file {FullName} with job id {jobGuid} for archive record id {archiveRecordId}", 
                        sourceFile.FullName, jobGuid, jobInfo.Request.Context.ArchiveRecordId);

                    var result = manager.Convert(jobGuid, sourceFile, context.Message.DestinationExtension, context.Message.VideoQuality, jobInfo.Request.Context);

                    Log.Information("Finished conversion for file {FullName} with job id {jobGuid} for archive record id {archiveRecordId}",
                        sourceFile.FullName, jobGuid, jobInfo.Request.Context.ArchiveRecordId);

                    await context.RespondAsync(new ConversionStartResult
                    {
                        ConvertedFileName = result.Name,
                        JobGuid = jobGuid,
                        Password = jobInfo.Result.Password,
                        Port = jobInfo.Result.Port,
                        UploadUrl = jobInfo.Result.UploadUrl,
                        User = jobInfo.Result.User
                    });
                }
                catch (Exception e)
                {
                    var message = e.Message;

                    try
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        message += $" (File: {sourceFile.FullName})";
                    }
                    catch
                    {
                        // Hier ist nichts zu tun                           
                    }

                    Log.Error(e, message);

                    await context.RespondAsync(new ConversionStartResult
                    {
                        IsInvalid = true,
                        ErrorMessage = e.Message
                    });
                }
            }
        }
    }
}