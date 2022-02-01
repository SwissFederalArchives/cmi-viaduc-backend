using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Manager.ExternalContent.Consumers;
using CMI.Manager.ExternalContent.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;

namespace CMI.Manager.ExternalContent
{
    /// <summary>
    ///     The ExternalContentService is configuring the bus and IoC container for the external content manager.
    /// </summary>
    public class ExternalContentService
    {
        private ContainerBuilder containerBuilder;
        private IBusControl bus;

        public ExternalContentService()
        {
            // Configure IoC Container
            containerBuilder = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        /// <summary>
        ///     Starts the ExternalContent Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public void Start()
        {
            Log.Information("ExternalContent service is starting");

            // Configure Bus
            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.ExternalContentService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(BusConstants.ManagementApiGetDigitizationOrderData,
                    ec => { ec.Consumer(ctx.Resolve<DigitizationOrderConsumer>); });
                cfg.ReceiveEndpoint(BusConstants.ManagementApiGetReportExternalContent,
                    ec => { ec.Consumer(ctx.Resolve<ReportExternalConsumer>); });
            });

            // Add the bus instance to the IoC container
            var container = containerBuilder.Build();
            bus = container.Resolve<IBusControl>();
            bus.Start();

            Log.Information("ExternalContent service started");
        }


        /// <summary>
        ///     Stops the ExternalContent Service.
        ///     Called by the service host when the service is stopped.
        /// </summary>
        public void Stop()
        {
            Log.Information("ExternalContent service is stopping");
            bus.Stop();
            Log.Information("ExternalContent service stopped");
            Log.CloseAndFlush();
        }
    }
}