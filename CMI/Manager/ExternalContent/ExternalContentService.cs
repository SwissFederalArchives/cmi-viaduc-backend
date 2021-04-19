using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Manager.ExternalContent.Consumers;
using CMI.Manager.ExternalContent.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Ninject;
using Serilog;

namespace CMI.Manager.ExternalContent
{
    /// <summary>
    ///     The ExternalContentService is configuring the bus and IoC container for the external content manager.
    /// </summary>
    public class ExternalContentService
    {
        private readonly StandardKernel kernel;
        private IBusControl bus;

        public ExternalContentService()
        {
            // Configure IoC Container
            kernel = ContainerConfigurator.Configure();
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
            bus = BusConfigurator.ConfigureBus(MonitoredServices.ExternalContentService, (cfg, host) =>
            {
                cfg.ReceiveEndpoint(BusConstants.ManagementApiGetDigitizationOrderData,
                    ec => { ec.Consumer(() => kernel.Get<DigitizationOrderConsumer>()); });

                cfg.UseSerilog();
            });

            // Add the bus instance to the IoC container
            kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();
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