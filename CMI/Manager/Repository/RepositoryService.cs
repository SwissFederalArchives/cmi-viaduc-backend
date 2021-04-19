using System;
using System.Reflection;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Repository.Consumer;
using CMI.Manager.Repository.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using GreenPipes;
using MassTransit;
using Ninject;
using Ninject.Activation;
using Serilog;

namespace CMI.Manager.Repository
{
    public class RepositoryService
    {
        private readonly StandardKernel kernel;
        private IBusControl bus;

        public RepositoryService()
        {
            // Configure IoC Container
            kernel = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        /// <summary>
        ///     Starts the Repository Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public void Start()
        {
            Log.Information("Repository service is starting");

            // Configure Bus
            var helper = new ParameterBusHelper();
            bus = BusConfigurator.ConfigureBus(MonitoredServices.RepositoryService, (cfg, host) =>
            {
                cfg.ReceiveEndpoint(BusConstants.RepositoryManagerDownloadPackageMessageQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<DownloadPackageConsumer>());
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });
                cfg.ReceiveEndpoint(BusConstants.RepositoryManagerArchiveRecordAppendPackageMessageQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<AppendPackageConsumer>());
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });
                cfg.ReceiveEndpoint(BusConstants.RepositoryManagerReadPackageMetadataMessageQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<ReadPackageMetadataConsumer>());
                    ec.UseRetry(retryPolicy => retryPolicy.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.MonitoringDirCheckQueue, ec => { ec.Consumer(() => kernel.Get<CheckDirConsumer>()); });

                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg, host);
                cfg.UseSerilog();
            });

            // Add the bus instance to the IoC container
            kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IRequestClient<GetArchiveRecordsForPackageRequest, GetArchiveRecordsForPackageResponse>>()
                .ToMethod(GetArchiveRecordsForPackageRequestClientCallback);

            bus.Start();

            Log.Information("Repository service started");
        }

        private IRequestClient<GetArchiveRecordsForPackageRequest, GetArchiveRecordsForPackageResponse>
            GetArchiveRecordsForPackageRequestClientCallback(IContext arg)
        {
            var serviceUrl = string.Format(BusConstants.IndexManagagerRequestBase, nameof(GetArchiveRecordsForPackageRequest));
            var requestTimeout = TimeSpan.FromMinutes(1);

            var client =
                new MessageRequestClient<GetArchiveRecordsForPackageRequest, GetArchiveRecordsForPackageResponse>(bus,
                    new Uri(bus.Address, serviceUrl), requestTimeout, null, BusConfigurator.ChangeResponseAddress);

            return client;
        }

        /// <summary>
        ///     Stops the Repository Service.
        ///     Called by the service host when the service is stopped.
        /// </summary>
        public void Stop()
        {
            Log.Information("Repository service is stopping");
            bus.Stop();
            Log.Information("Repository service stopped");
            Log.CloseAndFlush();
        }
    }
}