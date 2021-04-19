using System;
using System.Reflection;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Harvest.Consumers;
using CMI.Manager.Harvest.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using GreenPipes;
using MassTransit;
using Ninject;
using Ninject.Activation;
using Serilog;

namespace CMI.Manager.Harvest
{
    /// <summary>
    ///     The HarvestService is configuring the bus and IoC container.
    /// </summary>
    public class HarvestService
    {
        private readonly StandardKernel kernel;
        private IBusControl bus;

        public HarvestService()
        {
            // Configure IoC Container
            kernel = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        /// <summary>
        ///     Starts the Harvest Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public void Start()
        {
            Log.Information("Harvest service is starting");

            // Configure Bus
            bus = BusConfigurator.ConfigureBus(MonitoredServices.HarvestService, (cfg, host) =>
            {
                cfg.ReceiveEndpoint(BusConstants.HarvestManagerSyncArchiveRecordMessageQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<SyncArchiveRecordConsumer>());
                    // Retry for a maximum of 10 times with the following intervals
                    // 00:00:6      minInterval + 1 * intervalDelta
                    // 00:00:11     minInterval + 2 * intervalDelta
                    // 00:00:21     minInterval + 4 * intervalDelta
                    // 00:00:41     minInterval + 8 * intervalDelta
                    // 00:01:21     minInterval + 16 * intervalDelta
                    // 00:02:41     minInterval + 32 * intervalDelta
                    // 00:05:00     maxInterval
                    // 00:05:00
                    // 00:05:00
                    // 00:05:00
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.HarvestManagerArchiveRecordUpdatedEventQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<ArchiveRecordUpdatedConsumer>());
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.HarvestManagerArchiveRecordRemovedEventQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<ArchiveRecordRemovedConsumer>());
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.HarvestManagerResyncArchiveDatabaseMessageQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<ArchiveDatabaseResyncConsumer>());
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.ManagementApiGetHarvestStatusInfoRequestQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<HarvestStatusInfoConsumer>());
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.ManagementApiGetHarvestLogInfoRequestQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<HarvestLogInfoConsumer>());
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.MonitoringAisDbCheckQueue, ec => { ec.Consumer(() => kernel.Get<CheckAisDbConsumer>()); });

                cfg.UseSerilog();

                // Wire up the parameter manager
                var helper = new ParameterBusHelper();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg, host);
            });

            // Add the bus instance to the IoC container
            kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>>()
                .ToMethod(CreateFindArchiveRecordRequestClient);
            bus.Start();

            Log.Information("Harvest service started");
        }

        private IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse> CreateFindArchiveRecordRequestClient(IContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var client =
                new MessageRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>(bus,
                    new Uri(new Uri(BusConfigurator.Uri), BusConstants.IndexManagerFindArchiveRecordMessageQueue), requestTimeout, null,
                    BusConfigurator.ChangeResponseAddress);

            return client;
        }

        /// <summary>
        ///     Stops the Harvest Service.
        ///     Called by the service host when the service is stopped.
        /// </summary>
        public void Stop()
        {
            Log.Information("Harvest service is stopping");
            bus.Stop();
            Log.Information("Harvest service stopped");
            Log.CloseAndFlush();
        }
    }
}