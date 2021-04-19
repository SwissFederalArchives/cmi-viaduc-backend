using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Manager.Order.Properties;
using MassTransit;
using Serilog;
using Serilog.Context;

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

                var ep = await context.GetSendEndpoint(new Uri(bus.Address, BusConstants.AssetManagerTransformAssetMessageQueue));
                await ep.Send<ITransformAsset>(new TransformAsset
                {
                    AssetType = AssetType.Benutzungskopie,
                    OrderItemId = message.OrderItemId,
                    ArchiveRecordId = message.ArchiveRecordId,
                    FileName = new FileInfo(fileName).Name, // Only pass the name, as the AssetManager expects the files in a specific location
                    ProtectWithPassword = true,
                    RetentionCategory = CacheRetentionCategory.UsageCopyBenutzungskopie
                });
            }
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