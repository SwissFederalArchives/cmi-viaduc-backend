using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Harvest.Consumers;
using CMI.Manager.Harvest.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;
using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CMI.Manager.Harvest
{
    /// <summary>
    ///     The HarvestService is configuring the bus and IoC container.
    /// </summary>
    public class HarvestService
    {
        private readonly IServiceCollection services;
        private IBusControl bus;

        public HarvestService()
        {
            // Configure IoC Container
            services = ContainerConfigurator.Configure();
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
            BusConfigurator.ConfigureBusModern(services, MonitoredServices.HarvestService, AddConsumers, (context, cfg) =>
            {
                cfg.ReceiveEndpoint(BusConstants.HarvestManagerSyncArchiveRecordMessageQueue,
                    ec =>
                    {
                        ec.ConfigureConsumer<SyncArchiveRecordConsumer>(context);
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
                        BusConfigurator.SetPrefetchCountForEndpoint(ec);
                    });

                cfg.ReceiveEndpoint(BusConstants.HarvestManagerArchiveRecordUpdatedEventQueue, ec =>
                {
                    ec.ConfigureConsumer<ArchiveRecordUpdatedConsumer>(context);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.HarvestManagerArchiveRecordRemovedEventQueue, ec =>
                {
                    ec.ConfigureConsumer<ArchiveRecordRemovedConsumer>(context);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.HarvestManagerResyncArchiveDatabaseMessageQueue, ec =>
                {
                    ec.ConfigureConsumer<ArchiveDatabaseResyncConsumer>(context);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });

                cfg.ReceiveEndpoint(BusConstants.MonitoringAisDbCheckQueue, ec =>
                {
                    ec.ConfigureConsumer<CheckAisDbConsumer>(context);
                });
                cfg.ReceiveEndpoint(BusConstants.AisAccessTokensRequestQueue, ec =>
                {
                    ec.ConfigureConsumer<GetAisAccessTokensConsumer>(context);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                
                cfg.UseNewtonsoftJsonSerializer();
                // Wire up the parameter manager
                var helper = new ParameterBusHelper();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });

            var provider = services.BuildServiceProvider();
            bus = provider.GetRequiredService<IBusControl>();
            bus.Start();

            Log.Information("Harvest service started");
        }

        private void AddConsumers(IBusRegistrationConfigurator x)
        {
            // registers all IConsumer implementations in this assembly
            x.AddConsumers(Assembly.GetExecutingAssembly());
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