using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Manager.Order.Properties;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Order.Consumers
{
    public class BenutzungskopieAuftragErledigtConsumer : IConsumer<IBenutzungskopieAuftragErledigt>
    {
        private readonly IBus bus;


        public BenutzungskopieAuftragErledigtConsumer(IBus bus)
        {
            this.bus = bus;
        }

        public async Task Consume(ConsumeContext<IBenutzungskopieAuftragErledigt> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                var message = context.Message;
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", message.GetType().Name,
                    context.ConversationId);

                var fileName = MoveFileFromVecteurSftpToAssetManager(message.OrderItemId);

                var ep = await context.GetSendEndpoint(new Uri(bus.Address, BusConstants.AssetManagerPrepareForTransformation));
                await ep.Send(new PrepareForTransformationMessage
                {
                    AssetType = AssetType.Benutzungskopie,
                    OrderItemId = message.OrderItemId,
                    RepositoryPackage = CreateRepositoryPackage(fileName, message.ArchiveRecordId),
                    ProtectWithPassword = true,
                    RetentionCategory = CacheRetentionCategory.UsageCopyBenutzungskopie
                });
            }
        }

        /// <summary>
        /// Creates a repository package with file entries within the content folder.
        /// We are taking a shortcut here, because we are not building the full folder
        /// structure but use relative filenames.
        /// But in the end, this should work the same.
        /// </summary>
        /// <param name="fileName">The full file name of the zip file.</param>
        /// <param name="archiveRecordId">The archive record id</param>
        /// <returns></returns>
        private RepositoryPackage CreateRepositoryPackage(string fileName, string archiveRecordId)
        {
            const string contentFolderName = "content/";
            var package = new RepositoryPackage() {ArchiveRecordId = archiveRecordId, PackageFileName = (new FileInfo(fileName)).Name};
            using (var zipArchive = ZipFile.OpenRead(fileName))
            {

                // Sort the entries using their depth in the zip archive, so we can add
                foreach (var zipArchiveEntry in zipArchive.Entries.Where(f => f.FullName.StartsWith(contentFolderName) && f.Length > 0)
                    .OrderBy(e => e.FullName.Split('\\').Length).ThenBy(e => e.FullName))
                {
                    var relativeFileName = zipArchiveEntry.FullName.Substring(contentFolderName.Length);
                    package.Files.Add(new RepositoryFile
                    {
                        PhysicalName = relativeFileName,
                        SizeInBytes = zipArchiveEntry.Length,
                        Exported = true
                    });
                }
            }

            package.FileCount = package.Files.Count;
            package.SizeInBytes = package.Files.Sum(f => f.SizeInBytes);

            return package;
        }

        private static string MoveFileFromVecteurSftpToAssetManager(int orderItemId)
        {
            var sourceDirectory = Path.Combine(Settings.Default.VecteurSftpRoot, orderItemId.ToString());
            var vecteurFiles = Directory.GetFiles(sourceDirectory);
            if (vecteurFiles.Length == 1)
            {
                if (!Directory.Exists(Settings.Default.AssetManagerPickupPath))
                {
                    throw new InvalidOperationException("The specified Asset Manager Pickup path is not found");
                }

                var destinationFileName = Path.Combine(Settings.Default.AssetManagerPickupPath, Guid.NewGuid() + ".zip");
                File.Move(vecteurFiles.First(), destinationFileName);

                // Cleanup. Remove the directory the file was in.
                Directory.Delete(sourceDirectory);

                return destinationFileName;
            }

            if (vecteurFiles.Length == 0)
            {
                throw new InvalidOperationException($"No file found at {sourceDirectory}. Expected to find exactly one file.");
            }

            throw new InvalidOperationException($"More than one file found at {sourceDirectory}. Expected to find exactly one file.");
        }
    }
}