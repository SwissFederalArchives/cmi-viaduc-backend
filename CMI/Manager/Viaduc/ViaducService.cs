using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Manager.Viaduc.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;

namespace CMI.Manager.Viaduc
{
    public class ViaducService
    {
        private IBusControl bus;

        public void Start()
        {
            LogConfigurator.ConfigureForService();

            Log.Information("Viaduc service is starting");

            var containerBuilder = ContainerConfigurator.Configure();

            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.ViaducService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(BusConstants.ReadUserInformationQueue,
                    ec => { ec.Consumer(ctx.Resolve<ReadUserInformationConsumer>); }
                );
                cfg.ReceiveEndpoint(BusConstants.ReadStammdatenQueue,
                    ec => { ec.Consumer(ctx.Resolve<ReadStammdatenConsumer>); }
                );
            });

            var container = containerBuilder.Build();
            bus = container.Resolve<IBusControl>();
            bus.Start();

            Log.Information("Viaduc service started");
        }

        public void Stop()
        {
            Log.Information("Viaduc service is stopping.");
            bus.Stop();
            Log.Information("Viaduc service has stopped.");
            Log.CloseAndFlush();
        }
    }
}