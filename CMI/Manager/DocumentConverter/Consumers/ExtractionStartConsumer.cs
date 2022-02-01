using System;
using System.IO;
using System.Threading.Tasks;
using CMI.Contract.DocumentConverter;
using CMI.Manager.DocumentConverter.Properties;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.DocumentConverter.Consumers
{
    internal class ExtractionStartConsumer : IConsumer<ExtractionStartRequest>
    {
        private readonly IDocumentManager manager;
        private readonly SftpServer sftpServer;

        public ExtractionStartConsumer(SftpServer sftpServer, IDocumentManager manager)
        {
            this.sftpServer = sftpServer;
            this.manager = manager;
        }

        public async Task Consume(ConsumeContext<ExtractionStartRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                var jobGuid = context.Message.JobGuid;
                try
                {
                    Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                        nameof(ExtractionStartConsumer), context.ConversationId);

                    var jobInfo = sftpServer.GetJobInfo(jobGuid);

                    if (jobInfo == null)
                    {
                        Log.Error("Could not retrieve information about job with id {jobGuid}", jobGuid);
                        throw new InvalidOperationException($"Could not retrieve information about job with id {jobGuid}");
                    }

                    // Get source file
                    var sourceFile = new FileInfo(Path.Combine(Path.Combine(DocumentConverterSettings.Default.BaseDirectory, jobGuid,
                        new FileInfo(jobInfo.Request.FileNameWithExtension).Name)));

                    if (!sourceFile.Exists)
                    {
                        throw new FileNotFoundException(sourceFile.FullName);
                    }

                    Log.Information("Starting text extraction for file {FullName} with job id {jobGuid} for archive record id {archiveRecordId}", 
                        sourceFile.FullName, jobGuid, jobInfo.Request.Context.ArchiveRecordId);

                    var extractionResult = manager.ExtractText(jobGuid, sourceFile, jobInfo.Request.Context);

                    Log.Information("Finished text extraction for file {FullName} with job id {jobGuid} for archive record id {archiveRecordId}",
                        sourceFile.FullName, jobGuid, jobInfo.Request.Context.ArchiveRecordId);

                    // Depending on the OCR extraction result, we prepare the result
                    var result = new ExtractionStartResult();
                    if (!extractionResult.HasError)
                    {
                        result.Text = extractionResult.ToString();
                    }
                    else
                    {
                        result.IsInvalid = true;
                        result.ErrorMessage = extractionResult.ErrorMessage;
                    }

                    await context.RespondAsync(result);
                }
                catch (Exception e)
                {
                    Log.Error(e, e.Message);
                    await context.RespondAsync(new ExtractionStartResult
                    {
                        IsInvalid = true,
                        ErrorMessage = e.Message
                    });
                }
                finally
                {
                    sftpServer.RemoveJob(jobGuid);
                }
            }
        }
    }
}