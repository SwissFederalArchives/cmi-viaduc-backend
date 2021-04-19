using System;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Utilities.Common.Helpers;
using MassTransit;
using Nustache.Core;
using Serilog;

namespace CMI.Utilities.Template
{
    public class MailHelper : IMailHelper
    {
        private readonly IUserCulture userCulture = new UserCulture();

        public string TransformToHtml(string template, object dataSource, string language = "de")
        {
            var renderAction = new Func<string>(() =>
            {
                return Render.StringToString(template, dataSource, new RenderContextBehaviour
                {
                    HtmlEncoder = text =>
                    {
                        var encoded = Encoders.DefaultHtmlEncode(text);
                        return encoded.Replace("\r\n", "<br>").Replace("\n", "<br>");
                    }
                });
            });

            return userCulture.RunWithUserCultureInfo(language, renderAction);
        }


        public string TransformToText(string template, object dataSource, string language = "de")
        {
            var renderAction = new Func<string>(() =>
            {
                return Render.StringToString(template, dataSource, new RenderContextBehaviour {HtmlEncoder = text => text});
            });

            return userCulture.RunWithUserCultureInfo(language, renderAction);
        }

        public async Task SendEmail(IBus bus, EmailTemplate templateSetting, object dataContext, bool logAllowed = true)
        {
            try
            {
                var endpoint = await bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.NotificationManagerMessageQueue));

                var message = new EmailMessage
                {
                    From = TransformToText(templateSetting.From, dataContext),
                    To = TransformToText(templateSetting.To, dataContext),
                    CC = TransformToText(templateSetting.Cc, dataContext),
                    Bcc = TransformToText(templateSetting.Bcc, dataContext),
                    Subject = TransformToText(templateSetting.Subject, dataContext),
                    Body = TransformToHtml(templateSetting.Body, dataContext),
                    LogAllowed = logAllowed
                };

                await endpoint.Send<IEmailMessage>(message);
                Log.Information("Email sent");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An exception occurred while sending an EMail");
                throw;
            }
        }
    }
}