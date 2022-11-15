using Autofac;
using CMI.Contract.Monitoring;
using CMI.Manager.Monitoring.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;

namespace CMI.Manager.Monitoring
{
    public class MonitoringService
    {
        private readonly ContainerBuilder builder;
        private IBusControl bus;

        public MonitoringService()
        {
            // Configure IoC Container
            builder = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        public void Start()
        {
            BusConfigurator.ConfigureBus(builder, MonitoredServices.MonitoringService, (cfg, ctx) =>
            {
                cfg.UseNewtonsoftJsonSerializer();
            });
            var container = builder.Build();
            bus = container.Resolve<IBusControl>();
            bus.Start();
        }

        public void Stop()
        {
            bus.Stop();
        }
    }
}