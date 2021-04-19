using CMI.Contract.Monitoring;
using CMI.Manager.Monitoring.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Ninject;

namespace CMI.Manager.Monitoring
{
    public class MonitoringService
    {
        private readonly StandardKernel kernel;
        private IBusControl bus;

        public MonitoringService()
        {
            // Configure IoC Container
            kernel = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        public void Start()
        {
            bus = BusConfigurator.ConfigureBus(MonitoredServices.MonitoringService);

            // Add the bus instance to the IoC container
            kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();
            bus.Start();
        }

        public void Stop()
        {
            bus.Stop();
        }
    }
}