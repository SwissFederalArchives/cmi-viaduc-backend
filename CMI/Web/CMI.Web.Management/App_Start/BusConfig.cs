using System;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Utilities.Bus.Configuration;
using MassTransit;
using Ninject;
using Ninject.Activation;
using Serilog;

namespace CMI.Web.Management
{
    public class BusConfig
    {
        private static IBusControl bus;

        public static void Configure(IKernel kernel)
        {
            // Configure Bus
            bus = BusConfigurator.ConfigureBus(MonitoredServices.NotMonitored, (cfg, host) => { cfg.UseSerilog(); });

            kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope(); // Add the bus instance to the IoC container
            kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>>().ToMethod(CreateFindArchiveRecordRequestClient);

            bus.Start();

            Log.Information("CMI.Web.Management bus service started");
        }

        /// <summary>
        ///     Registers the download asset request/response constructur callback for ninject.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>IRequestClient&lt;DownloadAsset, DownloadAssetResult&gt;.</returns>
        public static IRequestClient<DownloadAssetRequest, DownloadAssetResult> RegisterDownloadAssetCallback(IContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var client = new MessageRequestClient<DownloadAssetRequest, DownloadAssetResult>(
                bus,
                new Uri(new Uri(BusConfigurator.Uri), BusConstants.WebApiDownloadAssetRequestQueue),
                requestTimeout);

            return client;
        }

        private static IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse> CreateFindArchiveRecordRequestClient(IContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var client = new MessageRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>(
                bus,
                new Uri(new Uri(BusConfigurator.Uri), BusConstants.IndexManagerFindArchiveRecordMessageQueue),
                requestTimeout);

            return client;
        }

        public static IRequestClient<GetElasticLogRecordsRequest, GetElasticLogRecordsResponse> CreateGetElasticLogRecordsRequestClient(
            IContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(15);
            var ttl = TimeSpan.FromMinutes(1);

            var client = new MessageRequestClient<GetElasticLogRecordsRequest, GetElasticLogRecordsResponse>(
                bus,
                new Uri(new Uri(BusConfigurator.Uri), BusConstants.IndexManagerGetElasticLogRecordsRequestQueue),
                requestTimeout,
                ttl);

            return client;
        }

        public static IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse> CreateDoesExistInCacheClient(IContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(5);
            var ttl = TimeSpan.FromMinutes(1);

            var client = new MessageRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse>(
                bus,
                new Uri(new Uri(BusConfigurator.Uri), BusConstants.CacheDoesExistRequestQueue),
                requestTimeout,
                ttl);

            return client;
        }
    }
}