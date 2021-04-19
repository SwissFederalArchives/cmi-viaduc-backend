using System;
using System.Web;
using System.Web.Http;
using CMI.Contract.Messaging;
using CMI.Utilities.Cache.Access;
using CMI.Utilities.ProxyClients.Order;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.App_Start;
using CMI.Web.Management.DependencyInjection;
using MassTransit;
using Microsoft.Web.Infrastructure.DynamicModuleHelper;
using Ninject;
using Ninject.Web.Common;
using Ninject.Web.Common.WebHost;
using Ninject.Web.WebApi;
using WebActivatorEx;

[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(NinjectWebCommon), "Start")]
[assembly: ApplicationShutdownMethod(typeof(NinjectWebCommon), "Stop")]

namespace CMI.Web.Management.App_Start
{
    public static class NinjectWebCommon
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();

        public static IKernel Kernel => bootstrapper.Kernel;

        /// <summary>
        ///     Starts the application
        /// </summary>
        public static void Start()
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
        }

        /// <summary>
        ///     Stops the application.
        /// </summary>
        public static void Stop()
        {
            var bus = Kernel.Get<IBusControl>();
            bus.StopAsync().ContinueWith(t => { bootstrapper.ShutDown(); });
        }

        /// <summary>
        ///     Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel(new ManagementModules());
            kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
            kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

            RegisterServices(kernel);

            GlobalConfiguration.Configuration.DependencyResolver = new NinjectDependencyResolver(kernel);
            return kernel;
        }

        private static void RegisterServices(IKernel kernel)
        {
            kernel.Bind<OrderManagerClient>().ToSelf();
            kernel.Bind<ExcelExportHelper>().ToSelf();
            kernel.Bind<ICacheHelper>().To<CacheHelper>().WithConstructorArgument("sftpLicenseKey", WebHelper.Settings["sftpLicenseKey"]);
            kernel.Bind<IRequestClient<GetElasticLogRecordsRequest, GetElasticLogRecordsResponse>>()
                .ToMethod(BusConfig.CreateGetElasticLogRecordsRequestClient);
            kernel.Bind<IRequestClient<DownloadAssetRequest, DownloadAssetResult>>()
                .ToMethod(BusConfig.RegisterDownloadAssetCallback);
            kernel.Bind<IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse>>()
                .ToMethod(BusConfig.CreateDoesExistInCacheClient);
        }
    }
}