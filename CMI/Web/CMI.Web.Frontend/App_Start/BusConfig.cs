using System;
using System.Reflection;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Utilities.Bus.Configuration;
using MassTransit;
using Ninject;
using Ninject.Activation;
using Serilog;

namespace CMI.Web.Frontend
{
    public class BusConfig
    {
        private static IBusControl bus;

        public static void Configure(IKernel kernel)
        {
            // Configure Bus
            var helper = new ParameterBusHelper();
            bus = BusConfigurator.ConfigureBus(MonitoredServices.NotMonitored, (cfg, host) =>
            {
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg, host);

                cfg.UseSerilog();
            });

            kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();

            bus.Start();

            Log.Information("CMI.Web.Frontend bus service started");
        }

        /// <summary>
        ///     Registers a request/response constructor callback for ninject.
        /// </summary>
        /// <typeparam name="T1">The type of the Request.</typeparam>
        /// <typeparam name="T2">The type of the Response.</typeparam>
        /// <param name="context">The context.</param>
        /// <param name="serviceUrl">The service URL.</param>
        /// <returns>IRequestClient&lt;T1, T2&gt;.</returns>
        private static IRequestClient<T1, T2> GetRequestClient<T1, T2>(IContext context, string serviceUrl) where T1 : class where T2 : class
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var client = new MessageRequestClient<T1, T2>(bus, new Uri(new Uri(BusConfigurator.Uri), serviceUrl), requestTimeout);

            return client;
        }

        /// <summary>
        ///     Registers the download asset request/response constructur callback for ninject.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>IRequestClient&lt;DownloadAsset, DownloadAssetResult&gt;.</returns>
        public static IRequestClient<DownloadAssetRequest, DownloadAssetResult> RegisterDownloadAssetCallback(IContext context)
        {
            return GetRequestClient<DownloadAssetRequest, DownloadAssetResult>(context, BusConstants.WebApiDownloadAssetRequestQueue);
        }

        /// <summary>
        ///     Registers the get asset status callback.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>IRequestClient&lt;GetAssetStatusRequest, GetAssetStatusResult&gt;.</returns>
        public static IRequestClient<GetAssetStatusRequest, GetAssetStatusResult> RegisterGetAssetStatusCallback(IContext context)
        {
            return GetRequestClient<GetAssetStatusRequest, GetAssetStatusResult>(context, BusConstants.WebApiGetAssetStatusRequestQueue);
        }

        /// <summary>
        ///     Registers the prepare asset callback.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns>IRequestClient&lt;PrepareAssetRequest, PrepareAssetResult&gt;.</returns>
        public static IRequestClient<PrepareAssetRequest, PrepareAssetResult> RegisterPrepareAssetCallback(IContext context)
        {
            return GetRequestClient<PrepareAssetRequest, PrepareAssetResult>(context, BusConstants.WebApiPrepareAssetRequestQueue);
        }
    }
}