using Autofac;
using CMI.Contract.Monitoring;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;

namespace CMI.Manager.Parameter
{
    public class ParameterService
    {
        private ContainerBuilder containerBuilder;

        public ParameterService()
        {
            LogConfigurator.ConfigureForService();
        }

        public static IBusControl ParameterBus { get; set; }

        public void Start()
        {
            containerBuilder = new ContainerBuilder();

            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.ParameterService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint("GetParameterQueue", ec => { ec.Consumer(() => new GetParameterRequestConsumer()); });
                cfg.ReceiveEndpoint(ec => { ec.Consumer(() => new GetParameterEventResponseConsumer()); });
                cfg.ReceiveEndpoint("SaveParameterQueue", ec => { ec.Consumer(() => new SaveParameterRequestConsumer()); });
                cfg.ReceiveEndpoint(ec => { ec.Consumer(() => new SaveParameterEventResponseConsumer()); });
                cfg.UseNewtonsoftJsonSerializer();
            });

            var container = containerBuilder.Build();
            ParameterBus = container.Resolve<IBusControl>();
            ParameterBus.Start();
            ParameterRequestResponseHelper.Start();
        }

        public void Stop()
        {
            ParameterBus.Stop();
        }
    }
}