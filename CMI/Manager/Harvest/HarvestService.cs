using System;
using System.Reflection;
using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Harvest.Consumers;
using CMI.Manager.Harvest.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;

namespace CMI.Manager.Harvest
{
    /// <summary>
    ///     The HarvestService is configuring the bus and IoC container.
    /// </summary>
    public class HarvestService
    {
        private readonly ContainerBuilder containerBuilder;
        private IBusControl bus;

        public HarvestService()
        {
            // Configure IoC Container
            containerBuilder = ContainerConfigurator.Configure();
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
            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.HarvestService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(BusConstants.HarvestManagerSyncArchiveRecordMessageQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<SyncArchiveRecordConsumer>);
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
                    ec.Consumer(ctx.Resolve<ArchiveRecordUpdatedConsumer>);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.HarvestManagerArchiveRecordRemovedEventQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<ArchiveRecordRemovedConsumer>);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.HarvestManagerResyncArchiveDatabaseMessageQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<ArchiveDatabaseResyncConsumer>);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.ManagementApiGetHarvestStatusInfoRequestQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<HarvestStatusInfoConsumer>);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.ManagementApiGetHarvestLogInfoRequestQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<HarvestLogInfoConsumer>);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });

                cfg.ReceiveEndpoint(BusConstants.MonitoringAisDbCheckQueue, ec => { ec.Consumer(ctx.Resolve<CheckAisDbConsumer>); });

                cfg.UseNewtonsoftJsonSerializer();
                // Wire up the parameter manager
                var helper = new ParameterBusHelper();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });

            containerBuilder.Register(CreateFindArchiveRecordRequestClient);
            var container = containerBuilder.Build();

            bus = container.Resolve<IBusControl>();
            bus.Start();

            Log.Information("Harvest service started");
        }

        private IRequestClient<FindArchiveRecordRequest> CreateFindArchiveRecordRequestClient(IComponentContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(1);
            return bus.CreateRequestClient<FindArchiveRecordRequest>(new Uri(new Uri(BusConfigurator.Uri), BusConstants.IndexManagerFindArchiveRecordMessageQueue), requestTimeout);
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