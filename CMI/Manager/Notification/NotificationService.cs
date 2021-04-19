using System;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Manager.Notification.Consumers;
using CMI.Manager.Notification.Infrastructure;
using CMI.Manager.Notification.Properties;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using GreenPipes;
using MassTransit;
using Ninject;
using Serilog;

namespace CMI.Manager.Notification
{
    public class NotificationService
    {
        private readonly StandardKernel kernel;
        private IBusControl bus;

        public NotificationService()
        {
            kernel = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        public void Start()
        {
            LogInformation("Notification service is starting");
            // Configure Bus
            bus = BusConfigurator.ConfigureBus(MonitoredServices.NotificationService, (cfg, host) =>
            {
                cfg.ReceiveEndpoint(BusConstants.NotificationManagerMessageQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<EmailMessageConsumer>());
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });

                cfg.UseSerilog();
            });

            // Add the bus instance to the IoC container
            kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();
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