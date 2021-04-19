using System;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Engine.Security;
using CMI.Manager.Asset.Mails;
using CMI.Utilities.Cache.Access;
using CMI.Utilities.Template;
using MassTransit;
using Serilog;
using Serilog.Context;

namespace CMI.Manager.Asset.Consumers
{
    public class DownloadAssetConsumer : IConsumer<DownloadAssetRequest>
    {
        private readonly IBus bus;
        private readonly ICacheHelper cacheHelper;
        private readonly IDataBuilder dataBuilder;
        private readonly IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse> doesExistInCacheClient;
        private readonly IMailHelper mailHelper;
        private readonly IParameterHelper parameterHelper;
        private readonly PasswordHelper passwordHelper;

        public DownloadAssetConsumer(ICacheHelper cacheHelper,
            IBus bus,
            IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse> doesExistInCacheClient,
            IParameterHelper parameterHelper,
            IMailHelper mailHelper,
            IDataBuilder dataBuilder,
            PasswordHelper passwordHelper)
        {
            this.cacheHelper = cacheHelper;
            this.bus = bus;
            this.doesExistInCacheClient = doesExistInCacheClient;
            this.parameterHelper = parameterHelper;
            this.mailHelper = mailHelper;
            this.dataBuilder = dataBuilder;
            this.passwordHelper = passwordHelper;
        }

        public async Task Consume(ConsumeContext<DownloadAssetRequest> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus",
                    nameof(DownloadAssetRequest), context.ConversationId);
                var message = context.Message;

                // Prüfen, ob die Datei im Cache vorhanden ist
                var id = message.AssetType == AssetType.Benutzungskopie ? message.OrderItemId.ToString() : message.ArchiveRecordId;
                var response = await doesExistInCacheClient.Request(new DoesExistInCacheRequest
                {
                    Id = id,
                    RetentionCategory = message.RetentionCategory
                });

                if (response.Exists)
                {
                    Log.Information("Found the asset for item id {id} with type {AssetType} in the cache.", id, message.AssetType);

                    if (message.AssetType == AssetType.Gebrauchskopie && message.RetentionCategory != CacheRetentionCategory.UsageCopyPublic)
                    {
                        await SendPasswordEmailGebrauchskopie(context);
                    }

                    if (message.AssetType == AssetType.Benutzungskopie && message.ForceSendPasswordMail)
                    {
                        await ForcedSendPasswordEmailBenutzungskopie(context, true);
                    }

                    var downloadAssetResult = new DownloadAssetResult
                    {
                        AssetDownloadLink = await cacheHelper.GetFtpUrl(bus, message.RetentionCategory, id)
                    };
                    Log.Information("Constructed downloadAssetResult");
                    await context.RespondAsync(downloadAssetResult);

                    return;
                }

                // The asset is not available
                await context.RespondAsync(new DownloadAssetResult
                {
                    AssetDownloadLink = null
                });
            }
        }

        private async Task SendPasswordEmailGebrauchskopie(ConsumeContext<DownloadAssetRequest> context)
        {
            try
            {
                var manager = parameterHelper.GetSetting<GebrauchskopiePasswort>();
                var message = context.Message;
                var dataContext = dataBuilder
                    .AddVe(message.ArchiveRecordId)
                    .AddUser(message.Recipient)
                    .AddValue("PasswortZipDatei", passwordHelper.GetHashPassword(message.ArchiveRecordId))
                    .AddValue("IstGebrauchskopieMitEinsichtsbewilligung", message.RetentionCategory == CacheRetentionCategory.UsageCopyEB)
                    .Create();

                await mailHelper.SendEmail(bus, manager, dataContext);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while sending an EMail");
                throw;
            }
        }

        private async Task ForcedSendPasswordEmailBenutzungskopie(ConsumeContext<DownloadAssetRequest> context, bool changeEmailRecipient)
        {
            try
            {
                var template = parameterHelper.GetSetting<AufbereiteteBenutzungskopieZumDownloadBereit>();
                var message = context.Message;
                var dataContext = dataBuilder
                    .AddAuftraege(new[] {Convert.ToInt32(message.OrderItemId)})
                    .AddUser(message.Recipient)
                    .AddValue("Fehlermeldung", "")
                    .AddValue("PasswortZipDatei", passwordHelper.GetHashPassword(message.OrderItemId.ToString()))
                    .Create();

                // Beim Download der Benutzungskopie muss dass Passwort an den auslösenden Benutzer geschickt werden.
                // Im Template ist die allgemeine bestellung@bar.admin.ch hinterlegt.
                if (changeEmailRecipient)
                {
                    template.To = dataContext.User.EmailAddress;
                }

                await mailHelper.SendEmail(bus, template, dataContext);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while sending an EMail");
                throw;
            }
        }
    }
}