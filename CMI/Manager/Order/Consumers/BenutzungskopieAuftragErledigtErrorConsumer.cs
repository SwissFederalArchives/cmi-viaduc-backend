using System;
using System.Threading.Tasks;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Manager.Order.Mails;
using CMI.Utilities.Template;
using MassTransit;
using Serilog;
using LogContext = Serilog.Context.LogContext;

namespace CMI.Manager.Order.Consumers
{
    public class BenutzungskopieAuftragErledigtErrorConsumer : IConsumer<IBenutzungskopieAuftragErledigt>
    {
        private readonly IBus bus;
        private readonly DataBuilder dataBuilder;
        private readonly IMailHelper mailHelper;
        private readonly IParameterHelper parameterHelper;

        public BenutzungskopieAuftragErledigtErrorConsumer(IBus bus, DataBuilder dataBuilder, IParameterHelper parameterHelper,
            IMailHelper mailHelper)
        {
            this.bus = bus;
            this.dataBuilder = dataBuilder;
            this.parameterHelper = parameterHelper;
            this.mailHelper = mailHelper;
        }

        public async Task Consume(ConsumeContext<IBenutzungskopieAuftragErledigt> context)
        {
            using (LogContext.PushProperty(nameof(context.ConversationId), context.ConversationId))
            {
                var message = context.Message;
                Log.Information("Received {CommandName} command with conversationId {ConversationId} from the bus", message.GetType().Name,
                    context.ConversationId);

                // Log failure of message receipt and send error mail to BAR admin
                Log.Error(
                    $"Received a BenutzungskopieAuftragErledigt event, but no file on SFTP server could be found matching the order number.\nThis should not happen.\nOrderId: {context.Message.OrderItemId}");
                await SendEmail(context);
            }
        }

        private async Task SendEmail(ConsumeContext<IBenutzungskopieAuftragErledigt> context)
        {
            try
            {
                var mailTemplate = parameterHelper.GetSetting<BenutzungskopieAuftragErledigtProblem>();
                var messageContext = context.Message;
                var dataContext = dataBuilder
                    .AddVe(messageContext.ArchiveRecordId)
                    .AddUser(messageContext.OrderUserId)
                    .AddBesteller(messageContext.OrderUserId)
                    .AddAuftraege(new[] {messageContext.OrderItemId})
                    .Create();

#if DEBUG
                mailTemplate.To = dataContext.User.EmailAddress;
#endif

                await mailHelper.SendEmail(bus, mailTemplate, dataContext);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while sending an EMail");
                throw;
            }
        }
    }
}