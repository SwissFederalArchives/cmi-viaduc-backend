using System;
using System.Reflection;
using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Utilities.Bus.Configuration;
using CMI.Web.Management.Consumers;
using MassTransit;
using Serilog;

namespace CMI.Web.Management
{
    public static class BusConfig
    {
        private static IBusControl bus;

        public static void RegisterBus(this ContainerBuilder builder)
        {
            var helper = new ParameterBusHelper();
            // Configure Bus
            BusConfigurator.ConfigureBus(builder, MonitoredServices.NotMonitored, (cfg, ctx) =>
            {
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
                cfg.ReceiveEndpoint(BusConstants.ManagementApiAbbyyProgressEventQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<AbbyyProgressEventConsumer>);
                });
                cfg.ReceiveEndpoint(BusConstants.ManagementApiDocumentConverterServiceStartedQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<DocumentConverterServiceStartedEventConsumer>);
                });
                cfg.UseNewtonsoftJsonSerializer();
            });
            builder.Register(c => CreateFindArchiveRecordRequestClient()).As<IRequestClient<FindArchiveRecordRequest>>();
        }

        public static void StartBus(IContainer container)
        {
            bus = container.Resolve<IBusControl>();
            bus.Start();
            Log.Information("CMI.Web.Management bus service started");
        }

        /// <summary>
        ///     Registers the download asset request/response constructur callback for a DI container.
        /// </summary>
        /// <returns>IRequestClient&lt;DownloadAsset, DownloadAssetResult&gt;.</returns>
        public static IRequestClient<DownloadAssetRequest> RegisterDownloadAssetCallback()
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var client = bus.CreateRequestClient<DownloadAssetRequest>(new Uri(new Uri(BusConfigurator.Uri), BusConstants.WebApiDownloadAssetRequestQueue),
                requestTimeout);

            return client;
        }

        private static IRequestClient<FindArchiveRecordRequest> CreateFindArchiveRecordRequestClient()
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var client = bus.CreateRequestClient<FindArchiveRecordRequest>(new Uri(new Uri(BusConfigurator.Uri), BusConstants.IndexManagerFindArchiveRecordMessageQueue),
                requestTimeout);

            return client;
        }

        public static IRequestClient<GetElasticLogRecordsRequest> CreateGetElasticLogRecordsRequestClient()
        {
            var requestTimeout = TimeSpan.FromMinutes(15);

            var client = bus.CreateRequestClient<GetElasticLogRecordsRequest>(new Uri(new Uri(BusConfigurator.Uri), BusConstants.IndexManagerGetElasticLogRecordsRequestQueue),
                requestTimeout);

            return client;
        }
        
        public static IRequestClient<DoesExistInCacheRequest> CreateDoesExistInCacheClient()
        {
            var requestTimeout = TimeSpan.FromMinutes(5);

            var client = bus.CreateRequestClient<DoesExistInCacheRequest>(new Uri(new Uri(BusConfigurator.Uri), BusConstants.CacheDoesExistRequestQueue),
                requestTimeout);

            return client;
        }
    }
}
