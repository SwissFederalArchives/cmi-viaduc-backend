using System;
using System.IO;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Manager.Asset.Properties;
using CMI.Utilities.Cache.Access;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Asset.Consumers
{
    public class TransformPackageConsumer : IConsumer<ITransformAsset>
    {
        private readonly IAssetManager assetManager;
        private readonly IBus bus;
        private readonly ICacheHelper cacheHelper;


        public TransformPackageConsumer(IAssetManager assetManager, ICacheHelper cacheHelper, IBus bus)
        {
            this.assetManager = assetManager;
            this.cacheHelper = cacheHelper;
            this.bus = bus;
        }

        public async Task Consume(ConsumeContext<ITransformAsset> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                var message = context.Message;
                var result = await Convert(message);

                // In case the conversion failed for a Benutzungskopie, the original file is zipped again and uploaded
                if (!result.Valid && message.AssetType == AssetType.Benutzungskopie)
                {
                    await UploadReZippedOriginalFile(message);
                }

                DeleteOriginalZipFile(message);

                // inform the world about the package
                await context.Publish<IAssetReady>(result);
            }
        }

        private async Task<AssetReady> Convert(ITransformAsset message)
        {
            var currentStatus = AufbereitungsStatusEnum.PaketTransferiert;
            var id = message.AssetType == AssetType.Gebrauchskopie ? message.ArchiveRecordId : message.OrderItemId.ToString();
            var assetReady = new AssetReady
            {
                ArchiveRecordId = message.ArchiveRecordId,
                OrderItemId = message.OrderItemId,
                CallerId = message.CallerId,
                AssetType = message.AssetType,
                Recipient = message.Recipient,
                RetentionCategory = message.RetentionCategory,
                PrimaerdatenAuftragId = message.PrimaerdatenAuftragId
            };

            try
            {
                // Let's convert the package
                var result = await assetManager.ConvertPackage(id, message.AssetType,
                    message.ProtectWithPassword, message.FileName, message.PackageId);

                currentStatus = AufbereitungsStatusEnum.AssetUmwandlungAbgeschlossen;
                await UpdatePrimaerdatenAuftragStatus(message, AufbereitungsServices.AssetService, currentStatus, result.ErrorMessage);

                if (!result.Valid)
                {
                    // We have failed in the conversion inform the world about the failed package
                    assetReady.Valid = false;
                    assetReady.ErrorMessage = result.ErrorMessage;
                    return assetReady;
                }

                // Upload to cache and then delete the package
                var uploadSuccess = await cacheHelper.SaveToCache(bus, message.RetentionCategory, result.FileName);

                var errorMessage = uploadSuccess ? null : $"Unable to upload file {result.FileName} to cache";
                currentStatus = AufbereitungsStatusEnum.ImCacheAbgelegt;
                await UpdatePrimaerdatenAuftragStatus(message, AufbereitungsServices.CacheService, currentStatus, errorMessage);

                File.Delete(result.FileName);

                if (!uploadSuccess)
                {
                    assetReady.Valid = false;
                    assetReady.ErrorMessage = errorMessage;
                    return assetReady;
                }

                assetReady.Valid = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in the conversion of the package for ID {id} and AssetType {AssetType}.", id, message.AssetType);
                assetReady.Valid = false;
                assetReady.ErrorMessage = $"Unexpected exception occured.\nException:\n{ex}";
                await UpdatePrimaerdatenAuftragStatus(message, AufbereitungsServices.CacheService, currentStatus, ex.Message);
            }

            return assetReady;
        }

        private async Task UpdatePrimaerdatenAuftragStatus(ITransformAsset message, AufbereitungsServices service, AufbereitungsStatusEnum newStatus,
            string errorText = null)
        {
            if (message.PrimaerdatenAuftragId > 0)
            {
                Log.Information("Auftrag mit Id {PrimaerdatenAuftragId} wurde im {service}-Service auf Status {Status} gesetzt.",
                    message.PrimaerdatenAuftragId, service.ToString(), newStatus.ToString());

                var ep = await bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.AssetManagerUpdatePrimaerdatenAuftragStatusMessageQueue));
                await ep.Send<IUpdatePrimaerdatenAuftragStatus>(new UpdatePrimaerdatenAuftragStatus
                {
                    PrimaerdatenAuftragId = message.PrimaerdatenAuftragId,
                    Service = service,
                    Status = newStatus,
                    ErrorText = errorText
                });
            }
        }

        private async Task UploadReZippedOriginalFile(ITransformAsset message)
        {
            // Upload original package encrypted
            try
            {
                var createdFileName =
                    assetManager.CreateZipFileWithPasswordFromFile(message.FileName, message.OrderItemId.ToString(), message.AssetType);

                var uploadSuccess = await cacheHelper.SaveToCache(bus, message.RetentionCategory, createdFileName);
                if (!uploadSuccess)
                {
                    Log.Error("Unable to upload original package file {fileName} to cache", message.FileName);
                }

                File.Delete(createdFileName);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception on upload original package file {fileName} to cache", message.FileName);
            }
        }

        private static void DeleteOriginalZipFile(ITransformAsset message)
        {
            // Delete the original file
            try
            {
                var originalFile = Path.Combine(Settings.Default.PickupPath, message.FileName);
                File.Delete(originalFile);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception on delete original package file {fileName}", message.FileName);
            }
        }
    }
}