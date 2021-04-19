using CMI.Contract.Monitoring;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;

namespace CMI.Manager.Parameter
{
    public class ParameterService
    {
        public ParameterService()
        {
            LogConfigurator.ConfigureForService();
        }

        public static IBusControl ParameterBus { get; set; }

        public void Start()
        {
            ParameterBus = BusConfigurator.ConfigureBus(MonitoredServices.ParameterService, (cfg, host) =>
            {
                cfg.ReceiveEndpoint("GetParameterQueue", ec => { ec.Consumer(() => new GetParameterRequestConsumer()); });
                cfg.ReceiveEndpoint(host, ec => { ec.Consumer(() => new GetParameterEventResponseConsumer()); });
                cfg.ReceiveEndpoint("SaveParameterQueue", ec => { ec.Consumer(() => new SaveParameterRequestConsumer()); });
                cfg.ReceiveEndpoint(host, ec => { ec.Consumer(() => new SaveParameterEventResponseConsumer()); });
            });

            ParameterBus.Start();
            ParameterRequestResponseHelper.Start();
        }

        public void Stop()
        {
            ParameterBus.Stop();
        }
    }
}