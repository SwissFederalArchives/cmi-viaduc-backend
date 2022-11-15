using System;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;
using Serilog.Core.Enrichers;

namespace CMI.Manager.Repository.Consumer
{
    public class DownloadPackageConsumer : IConsumer<IDownloadPackage>
    {
        private readonly IBus bus;
        private readonly IRepositoryManager repositoryManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="DownloadPackageConsumer" /> class.
        /// </summary>
        /// <param name="repositoryManager">The repository manager.</param>
        /// <param name="bus">The bus.</param>
        public DownloadPackageConsumer(IRepositoryManager repositoryManager, IBus bus)
        {
            this.repositoryManager = repositoryManager;
            this.bus = bus;
        }

        public async Task Consume(ConsumeContext<IDownloadPackage> context)
        {
            var conversationEnricher = new PropertyEnricher(nameof(context.ConversationId), context.ConversationId);
            var archiveRecordIdEnricher = new PropertyEnricher(nameof(context.Message.ArchiveRecordId), context.Message.ArchiveRecordId);
            var packageIdEnricher = new PropertyEnricher(nameof(context.Message.PackageId), context.Message.PackageId);

            using (LogContext.Push(conversationEnricher, archiveRecordIdEnricher, packageIdEnricher))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", nameof(IDownloadPackage),
                    context.ConversationId);

                // Get the package from the repository
                // We are not waiting for it to end, because we want to free the consumer as early as possible
                var packageId = context.Message.PackageId;
                var archiveRecordId = context.Message.ArchiveRecordId;
                var result = await repositoryManager.GetPackage(packageId, archiveRecordId, context.Message.PrimaerdatenAuftragId);

                // Do we have a valid package?
                if (result.Success && result.Valid)
                {
                    // Forward the downloaded package to the asset manager for transformation
                    var endpoint = await context.GetSendEndpoint(new Uri(bus.Address, BusConstants.AssetManagerPrepareForTransformation));

                    await endpoint.Send(new PrepareForTransformationMessage
                    {
                        AssetType = AssetType.Gebrauchskopie,
                        CallerId = context.Message.CallerId,
                        RetentionCategory = context.Message.RetentionCategory,
                        Recipient = context.Message.Recipient,
                        Language = context.Message.Language,
                        ProtectWithPassword = context.Message.RetentionCategory != CacheRetentionCategory.UsageCopyPublic,
                        PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId,
                        RepositoryPackage = result.PackageDetails
                    });

                    // also publish the event, that the package is downloaded
                    await context.Publish<IPackageDownloaded>(
                        new
                        {
                            PackageInfo = result
                        });
                }
                else
                {
                    // Publish the download asset failure event
                    await context.Publish<IAssetReady>(new AssetReady
                    {
                        Valid = false,
                        ErrorMessage = result.ErrorMessage,
                        AssetType = AssetType.Gebrauchskopie,
                        CallerId = context.Message.CallerId,
                        ArchiveRecordId = context.Message.ArchiveRecordId,
                        RetentionCategory = context.Message.RetentionCategory,
                        Recipient = context.Message.Recipient,
                        PrimaerdatenAuftragId = context.Message.PrimaerdatenAuftragId
                    });
                }
            }
        }
    }
}