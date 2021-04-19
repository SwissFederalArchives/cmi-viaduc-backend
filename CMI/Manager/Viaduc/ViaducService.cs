using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
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

            bus = BusConfigurator.ConfigureBus(MonitoredServices.ViaducService, (cfg, host) =>
            {
                cfg.ReceiveEndpoint(BusConstants.ReadUserInformationQueue,
                    ec => { ec.Consumer(() => new ReadUserInformationConsumer()); }
                );
                cfg.ReceiveEndpoint(BusConstants.ReadStammdatenQueue,
                    ec => { ec.Consumer(() => new ReadStammdatenConsumer()); }
                );
            });

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