using System;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Engine.Security;
using CMI.Manager.Asset.Mails;
using CMI.Utilities.Template;
using MassTransit;
using Serilog;
using Serilog.Context;
using Serilog.Core.Enrichers;

namespace CMI.Manager.Asset.Consumers
{
    /// <summary>
    ///     In order to reliably unregister preparation jobs from the queue, we
    ///     listen to our "own" event, no matter from where it will be initiated.
    /// </summary>
    /// <seealso cref="IAssetReady" />
    public class AssetReadyConsumer : IConsumer<IAssetReady>
    {
        private readonly IAssetManager assetManager;
        private readonly IBus bus;
        private readonly IDataBuilder dataBuilder;
        private readonly IMailHelper mailHelper;
        private readonly IParameterHelper parameterHelper;
        private readonly PasswordHelper passwordHelper;

        public AssetReadyConsumer(IAssetManager assetManager, IParameterHelper parameterHelper, IBus bus, IMailHelper mailHelper,
            IDataBuilder dataBuilder, PasswordHelper passwordHelper)
        {
            this.assetManager = assetManager;
            this.parameterHelper = parameterHelper;
            this.bus = bus;
            this.mailHelper = mailHelper;
            this.dataBuilder = dataBuilder;
            this.passwordHelper = passwordHelper;
        }

        public async Task Consume(ConsumeContext<IAssetReady> context)
        {
            var conversationEnricher = new PropertyEnricher(nameof(context.ConversationId), context.ConversationId);
            var archiveRecordIdEnricher = new PropertyEnricher(nameof(context.Message.ArchiveRecordId), context.Message.ArchiveRecordId);

            using (LogContext.Push(conversationEnricher, archiveRecordIdEnricher))
            {
                Log.Information("Received {CommandName} command.", nameof(IAssetReady));

                switch (context.Message.AssetType)
                {
                    case AssetType.Gebrauchskopie:

                        await assetManager.UnregisterJobFromPreparationQueue(context.Message.PrimaerdatenAuftragId);

                        if (context.Message.Valid)
                        {
                            await SendMailGebrauchskopieOk(context);
                        }
                        else
                        {
                            await SendMailGebrauchskopieProblem(context);
                        }

                        break;

                    case AssetType.Benutzungskopie:

                        if (context.Message.Valid)
                        {
                            await SendMailBenutzungskopieOk(context);
                        }
                        else
                        {
                            await SendMailBenutzungskopieProblem(context);
                        }

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private async Task SendMailGebrauchskopieOk(ConsumeContext<IAssetReady> context)
        {
            try
            {
                var template = parameterHelper.GetSetting<GebrauchskopieZumDownloadBereit>();
                var message = context.Message;
                var dataContext = dataBuilder
                    .AddVe(message.ArchiveRecordId)
                    .AddUser(message.Recipient)
                    .AddValue("PasswortZipDatei", passwordHelper.GetHashPassword(message.ArchiveRecordId))
                    .AddValue("IstGebrauchskopieÖffentlich", message.RetentionCategory == CacheRetentionCategory.UsageCopyPublic)
                    .AddValue("IstGebrauchskopieEb", message.RetentionCategory == CacheRetentionCategory.UsageCopyEB)
                    .AddValue("IstGebrauchskopieAb", message.RetentionCategory == CacheRetentionCategory.UsageCopyAB)
                    .AddValue("IstGebrauchskopieBarOderAs", message.RetentionCategory == CacheRetentionCategory.UsageCopyBarOrAS)
                    .Create();

                await mailHelper.SendEmail(bus, template, dataContext);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while sending an EMail");
                throw;
            }
        }

        private async Task SendMailGebrauchskopieProblem(ConsumeContext<IAssetReady> context)
        {
            try
            {
                var template = parameterHelper.GetSetting<GebrauchskopieErstellenProblem>();
                var dataContext = dataBuilder
                    .AddVe(context.Message.ArchiveRecordId)
                    .AddUser(context.Message.Recipient)
                    .Create();

                await mailHelper.SendEmail(bus, template, dataContext);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while sending an EMail");
                throw;
            }
        }

        private async Task SendMailBenutzungskopieOk(ConsumeContext<IAssetReady> context)
        {
            try
            {
                var template = parameterHelper.GetSetting<AufbereiteteBenutzungskopieZumDownloadBereit>();
                var message = context.Message;
                var dataContext = dataBuilder
                    .AddAuftraege(new[] {Convert.ToInt32(message.OrderItemId)})
                    .AddValue("Fehlermeldung", message.ErrorMessage)
                    .AddValue("PasswortZipDatei", passwordHelper.GetHashPassword(message.OrderItemId.ToString()))
                    .Create();

                await mailHelper.SendEmail(bus, template, dataContext);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while sending an EMail");
                throw;
            }
        }

        private async Task SendMailBenutzungskopieProblem(ConsumeContext<IAssetReady> context)
        {
            try
            {
                var template = parameterHelper.GetSetting<AufbereitetenBenutzungskopieProblem>();
                var message = context.Message;
                var dataContext = dataBuilder
                    .AddAuftraege(new[] {Convert.ToInt32(message.OrderItemId)})
                    .AddValue("Fehlermeldung", message.ErrorMessage)
                    .AddValue("PasswortZipDatei", passwordHelper.GetHashPassword(message.OrderItemId.ToString()))
                    .Create();

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