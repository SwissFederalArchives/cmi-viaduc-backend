using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Manager.Notification.Properties;
using MassTransit;
using Serilog;

namespace CMI.Manager.Notification.Consumers
{
    public class EmailMessageConsumer : IConsumer<IEmailMessage>
    {
        private readonly IBus bus;
        private readonly IParameterHelper parameterHelper;

        public EmailMessageConsumer(IBus bus, IParameterHelper parameterHelper)
        {
            this.bus = bus;
            this.parameterHelper = parameterHelper;
        }

        public Task Consume(ConsumeContext<IEmailMessage> context)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(context.Message.To))
                {
                    Log.Error("Recipient must not be null or empty");
                    return Task.CompletedTask;
                }

                var client = new SmtpClient
                {
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Port = (int) NotificationSettings.Default.Port
                };

                if (!string.IsNullOrEmpty(NotificationSettings.Default.Host))
                {
                    client.Host = NotificationSettings.Default.Host;
                }

                if (!string.IsNullOrEmpty(NotificationSettings.Default.UserName) &&
                    !string.IsNullOrEmpty(NotificationSettings.Default.Password))
                {
                    client.Credentials = new NetworkCredential(NotificationSettings.Default.UserName,
                        NotificationSettings.Default.Password);
                }

                var mailFromAdress = string.IsNullOrEmpty(context.Message.From)
                    ? NotificationSettings.Default.DefaultFromAddress
                    : context.Message.From;

                var mail = new MailMessage(mailFromAdress, context.Message.To)
                {
                    IsBodyHtml = true,
                    Subject = context.Message.Subject,
                    Body = context.Message.Body
                };

                if (!string.IsNullOrWhiteSpace(context.Message.CC))
                {
                    mail.CC.Add(context.Message.CC);
                }

                if (!string.IsNullOrWhiteSpace(context.Message.Bcc))
                {
                    mail.Bcc.Add(context.Message.Bcc);
                }

                mail.Priority = context.Message.Priority;

                client.Send(mail);

                if (context.Message.LogAllowed)
                {
                    Log.Information(
                        $"Mail to '{context.Message.To}' and subject '{context.Message.Subject}' successfully sent");
                }
            }
            catch (SmtpException ex)
            {
                Log.Error(ex.ToString());
            }
            catch (Exception e)
            {
                Log.Error(e.ToString());
                throw;
            }

            return Task.CompletedTask;
        }
    }
}