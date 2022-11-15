using System;
using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Manager.Notification.Consumers;
using CMI.Manager.Notification.Infrastructure;
using CMI.Manager.Notification.Properties;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;

namespace CMI.Manager.Notification
{
    public class NotificationService
    {
        private readonly ContainerBuilder containerBuilder;
        private IBusControl bus;

        public NotificationService()
        {
            containerBuilder = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        public void Start()
        {
            LogInformation("Notification service is starting");
            // Configure Bus
            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.NotificationService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(BusConstants.NotificationManagerMessageQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<EmailMessageConsumer>);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.UseNewtonsoftJsonSerializer();
            });
            var container = containerBuilder.Build();
            bus = container.Resolve<IBusControl>();
            bus.Start();

            PublishStartupTestMailIfNeccessary();

            LogInformation("Notification service started");
        }

        private void PublishStartupTestMailIfNeccessary()
        {
            if (string.IsNullOrWhiteSpace(NotificationSettings.Default.StartupTestTo))
            {
                LogInformation("No startup test mail sent because setting 'StartupTestTo' is null or whitespaces");
                return;
            }

            var message = new EmailMessage
            {
                To = NotificationSettings.Default.StartupTestTo,
                Subject = NotificationSettings.Default.StartupTestSubject,
                Body = NotificationSettings.Default.StartupTestBody
            };

            LogInformation(
                $"Startup test mail sent with the following settings: to='{message.To}', subject='{message.Subject}', body='{message.Body}'");

            SendMessage(message);
        }

        private void LogInformation(string message)
        {
            Log.Information(message);
        }

        private void SendMessage(EmailMessage emailMessage)
        {
            var ep = bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.NotificationManagerMessageQueue));
            ep.Result.Send(emailMessage);
        }

        public void Stop()
        {
            LogInformation("Notification service is stopping");
            bus.Stop();
            LogInformation("Notification service stopped");
            Log.CloseAndFlush();
        }
    }
}