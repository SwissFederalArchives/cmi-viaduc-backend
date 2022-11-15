using System;
using System.Reflection;
using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Utilities.Bus.Configuration;
using MassTransit;
using Serilog;

namespace CMI.Web.Frontend
{
    public static class BusConfig
    {
        private static IBusControl bus;

        public static void RegisterBus(this ContainerBuilder builder)
        {
            // Configure Bus
            var helper = new ParameterBusHelper();
            BusConfigurator.ConfigureBus(builder, MonitoredServices.NotMonitored, (cfg, ctx) =>
            {
                cfg.UseNewtonsoftJsonSerializer();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });
        }

        public static void StartBus(IContainer container)
        {
            bus = container.Resolve<IBusControl>();
            bus.Start();
            Log.Information("CMI.Web.Frontend bus service started");
        }

        /// <summary>
        ///     Registers a request/response constructor callback for a DI container.
        /// </summary>
        /// <typeparam name="T1">The type of the Request.</typeparam>
        /// <param name="serviceUrl">The service URL.</param>
        /// <returns>IRequestClient&lt;T1, T2&gt;.</returns>
        private static IRequestClient<T1> GetRequestClient<T1>(string serviceUrl) where T1 : class
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var client = bus.CreateRequestClient<T1>(new Uri(new Uri(BusConfigurator.Uri), serviceUrl), requestTimeout);
            return client;
        }

        /// <summary>
        ///     Registers the download asset request/response constructur callback for a DI containter.
        /// </summary>
        /// <returns>IRequestClient&lt;DownloadAsset, DownloadAssetResult&gt;.</returns>
        public static IRequestClient<DownloadAssetRequest> RegisterDownloadAssetCallback()
        {
            return GetRequestClient<DownloadAssetRequest>(BusConstants.WebApiDownloadAssetRequestQueue);
        }

        /// <summary>
        ///     Registers the get asset status callback.
        /// </summary>
        /// <returns>IRequestClient&lt;GetAssetStatusRequest, GetAssetStatusResult&gt;.</returns>
        public static IRequestClient<GetAssetStatusRequest> RegisterGetAssetStatusCallback()
        {
            return GetRequestClient<GetAssetStatusRequest>(BusConstants.WebApiGetAssetStatusRequestQueue);
        }

        /// <summary>
        ///     Registers the prepare asset callback.
        /// </summary>
        /// <returns>IRequestClient&lt;PrepareAssetRequest, PrepareAssetResult&gt;.</returns>
        public static IRequestClient<PrepareAssetRequest> RegisterPrepareAssetCallback()
        {
            return GetRequestClient<PrepareAssetRequest>(BusConstants.WebApiPrepareAssetRequestQueue);
        }


        /// <summary>
        ///     Registers the start onboarding process callback
        /// </summary>
        public static IRequestClient<StartOnboardingProcessRequest> RegisterStartOnboardingProcessClient()
        {
            return GetRequestClient<StartOnboardingProcessRequest>(BusConstants.OnboardingManagerStartProcessMessageQueue);
        }

        /// <summary>
        ///     Registers the start onboarding handle callback
        /// </summary>
        public static IRequestClient<HandleOnboardingCallbackRequest> RegisterHandleOnboardingCallbackClient()
        {
            return GetRequestClient<HandleOnboardingCallbackRequest>(BusConstants.OnboardingManagerHandleCallbackMessageQueue);
        }
    }
}