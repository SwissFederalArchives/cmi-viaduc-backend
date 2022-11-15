using System;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;
using Serilog.Core.Enrichers;

namespace CMI.Manager.Repository.Consumer
{
    public class ReadPackageMetadataConsumer : IConsumer<IArchiveRecordAppendPackageMetadata>
    {
        private readonly IRepositoryManager repositoryManager;

        public ReadPackageMetadataConsumer(IRepositoryManager repositoryManager)
        {
            this.repositoryManager = repositoryManager;
        }

        public async Task Consume(ConsumeContext<IArchiveRecordAppendPackageMetadata> context)
        {
            var conversationEnricher = new PropertyEnricher(nameof(context.ConversationId), context.ConversationId);
            var archiveRecordIdEnricher = new PropertyEnricher(nameof(context.Message.ArchiveRecord.ArchiveRecordId),
                context.Message.ArchiveRecord?.ArchiveRecordId);

            using (LogContext.Push(conversationEnricher, archiveRecordIdEnricher))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(IArchiveRecordAppendPackage), context.ConversationId);

                if (context.Message.ArchiveRecord != null)
                {
                    // Get the package from the repository
                    var result = repositoryManager.ReadPackageMetadata(context.Message.ArchiveRecord.Metadata.PrimaryDataLink,
                        context.Message.ArchiveRecord.ArchiveRecordId);

                    // Inform the world about the created package
                    if (result != null && result.Success && result.Valid)
                    {
                        // Add the metadata to the archive record.
                        context.Message.ArchiveRecord.PrimaryData.Add(result.PackageDetails);
                        Log.Information("Package metadata extraction was successful for packageId {packageId}", result.PackageDetails.PackageId);

                        var endpoint = await context.GetSendEndpoint(new Uri(context.SourceAddress,
                            BusConstants.AssetManagerSchdeduleForPackageSyncMessageQueue));
                        await endpoint.Send<IScheduleForPackageSync>(new
                        {
                            Workload = new ArchiveRecordAppendPackage
                            {
                                MutationId = context.Message.MutationId,
                                ArchiveRecord = context.Message.ArchiveRecord,
                                ElasticRecord = context.Message.ElasticRecord
                            }
                        });
                    }
                    else
                    {
                        // If package creation was not successful, stop syncing here and return failure.
                        Log.Error(
                            "Failed to extract primary metadata from repository for archiveRecord with conversationId {ConversationId} with message {ErrorMessage}",
                            context.ConversationId, result?.ErrorMessage);
                        await context.Publish<IArchiveRecordUpdated>(new
                        {
                            context.Message.MutationId,
                            context.Message.ArchiveRecord.ArchiveRecordId,
                            ActionSuccessful = false,
                            result?.ErrorMessage
                        });
                    }
                }
            }
        }
    }
}