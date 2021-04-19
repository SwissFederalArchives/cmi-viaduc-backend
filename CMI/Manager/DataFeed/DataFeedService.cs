using System.Threading.Tasks;
using CMI.Contract.Monitoring;
using CMI.Manager.DataFeed.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Ninject;
using Quartz;
using Serilog;

namespace CMI.Manager.DataFeed
{
    public class DataFeedService
    {
        private readonly StandardKernel kernel;
        private IBusControl bus;
        private IScheduler scheduler;

        /// <summary>
        ///     The data feed service uses a timer to poll the mutation queue for any pending changes.
        ///     Pending changes are then put on the bus for further processing.
        /// </summary>
        public DataFeedService()
        {
            // Configure IoC Container
            kernel = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        /// <summary>
        ///     Starts the DataFeed Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public async Task Start()
        {
            Log.Information("DataFeed service is starting");
            scheduler = await SchedulerConfigurator.Configure(kernel);

            // Configure Bus
            bus = BusConfigurator.ConfigureBus(MonitoredServices.DataFeedService, (cfg, host) => { cfg.UseSerilog(); });

            // Add the bus instance to the IoC container
            kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();
            bus.Start();

            // Start the timer
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
            var token = kernel.Get<ICancelToken>();
            token.Cancel();

            // Stop the scheduler and wait until any running jobs have completed
            scheduler.Shutdown(true);

            bus.Stop();

            Log.Information("DataFeed service stopped");
            Log.CloseAndFlush();
        }
    }
}