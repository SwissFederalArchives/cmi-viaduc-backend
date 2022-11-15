using System;
using System.Reflection;
using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Repository.Consumer;
using CMI.Manager.Repository.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;

namespace CMI.Manager.Repository
{
    public class RepositoryService
    {
        private readonly ContainerBuilder containerBuilder;
        private IBusControl bus;

        public RepositoryService()
        {
            // Configure IoC Container
            containerBuilder = ContainerConfigurator.Configure();
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
            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.RepositoryService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(BusConstants.RepositoryManagerDownloadPackageMessageQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<DownloadPackageConsumer>);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });
                cfg.ReceiveEndpoint(BusConstants.RepositoryManagerArchiveRecordAppendPackageMessageQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<AppendPackageConsumer>);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });
                cfg.ReceiveEndpoint(BusConstants.RepositoryManagerReadPackageMetadataMessageQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<ReadPackageMetadataConsumer>);
                    ec.UseRetry(retryPolicy => retryPolicy.Exponential(3, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.MonitoringDirCheckQueue, ec => { ec.Consumer(ctx.Resolve<CheckDirConsumer>); });
                cfg.UseNewtonsoftJsonSerializer();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });

            containerBuilder.Register(GetArchiveRecordsForPackageRequestClientCallback);
            var container = containerBuilder.Build();
            bus = container.Resolve<IBusControl>();
            bus.Start();

            Log.Information("Repository service started");
        }

        private IRequestClient<GetArchiveRecordsForPackageRequest>
            GetArchiveRecordsForPackageRequestClientCallback(IComponentContext arg)
        {
            var serviceUrl = string.Format(BusConstants.IndexManagagerRequestBase, nameof(GetArchiveRecordsForPackageRequest));
            var requestTimeout = TimeSpan.FromMinutes(1);

            return bus.CreateRequestClient<GetArchiveRecordsForPackageRequest>(new Uri(bus.Address, serviceUrl), requestTimeout);
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