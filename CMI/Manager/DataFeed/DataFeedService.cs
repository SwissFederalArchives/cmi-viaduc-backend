using System.Threading.Tasks;
using Autofac;
using CMI.Contract.Monitoring;
using CMI.Manager.DataFeed.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Quartz;
using Serilog;

namespace CMI.Manager.DataFeed
{
    public class DataFeedService
    {
        private IContainer container;
        private ContainerBuilder containerBuilder;
        private IBusControl bus;
        private IScheduler scheduler;

        /// <summary>
        ///     The data feed service uses a timer to poll the mutation queue for any pending changes.
        ///     Pending changes are then put on the bus for further processing.
        /// </summary>
        public DataFeedService()
        {
            // Configure IoC Container
            containerBuilder = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        /// <summary>
        ///     Starts the DataFeed Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public async Task Start()
        {
            Log.Information("DataFeed service is starting");

            // Configure Bus
            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.DataFeedService, (cfg, ctx) => { });
            container = containerBuilder.Build();
            bus = container.Resolve<IBusControl>();
            bus.Start();

            // Start the timer
            scheduler = await SchedulerConfigurator.Configure(container);

            Log.Verbose("Starting scheduler");
            await scheduler.Start();


            Log.Information("DataFeed service started");
        }

        /// <summary>
        ///     Stops the DataFeed Service.
        ///     Called by the service host when the service is stopped.
        /// </summary>
        public void Stop()
        {
            Log.Information("DataFeed service is stopping");

            // Get the singleton JobCancelToken and cancel any running job
            var token = container.Resolve<ICancelToken>();
            token.Cancel();

            // Stop the scheduler and wait until any running jobs have completed
            scheduler.Shutdown(true);

            bus.Stop();

            Log.Information("DataFeed service stopped");
            Log.CloseAndFlush();
        }
    }
}