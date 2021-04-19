using System;
using System.Reflection;
using System.Threading;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Index.Consumer;
using CMI.Manager.Index.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using GreenPipes;
using MassTransit;
using Ninject;
using Serilog;

namespace CMI.Manager.Index
{
    /// <summary>
    ///     The index service is configuring the bus and the <see cref="IndexManager" />
    /// </summary>
    public class IndexService
    {
        private readonly StandardKernel kernel;
        private IBusControl bus;

        // ReSharper disable once NotAccessedField.Local 
        private Timer deleteOldLogIndexesTimer;


        public IndexService()
        {
            // Configure IoC Container
            kernel = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        /// <summary>
        ///     Starts the Index Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public void Start()
        {
            Log.Information("Index service is starting");

            // Configure Bus
            var helper = new ParameterBusHelper();
            bus = BusConfigurator.ConfigureBus(MonitoredServices.IndexService, (cfg, host) =>
            {
                cfg.ReceiveEndpoint(BusConstants.IndexManagerUpdateArchiveRecordMessageQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<UpdateArchiveRecordConsumer>());
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.IndexManagerRemoveArchiveRecordMessageQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<RemoveArchiveRecordConsumer>());
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.IndexManagerFindArchiveRecordMessageQueue,
                    ec => { ec.Consumer(() => kernel.Get<FindArchiveRecordConsumer>()); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.IndexManagagerRequestBase, nameof(GetArchiveRecordsForPackageRequest)),
                    ec =>
                    {
                        ec.Consumer(() =>
                            kernel.Get<SimpleConsumer<GetArchiveRecordsForPackageRequest, GetArchiveRecordsForPackageResponse, IIndexManager>>());
                    });

                cfg.ReceiveEndpoint(BusConstants.IndexManagerUpdateIndivTokensMessageQueue,
                    ec => { ec.Consumer(() => kernel.Get<UpdateIndivTokensConsumer>()); });
                cfg.ReceiveEndpoint(BusConstants.MonitoringElasticSearchTestQueue,
                    ec => { ec.Consumer(() => kernel.Get<TestElasticSearchRequestConsumer>()); });
                cfg.ReceiveEndpoint(BusConstants.IndexManagerGetElasticLogRecordsRequestQueue,
                    ec =>
                    {
                        ec.Consumer(() =>
                            kernel.Get<SimpleConsumer<GetElasticLogRecordsRequest, GetElasticLogRecordsResponse, IElasticLogManager>>());
                    });

                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg, host);
                cfg.UseSerilog();
            });

            // Add the bus instance to the IoC container
            kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IParameterHelper>().To<ParameterHelper>();
            bus.Start();

            var elasticLogManager = kernel.Get<IElasticLogManager>();
            deleteOldLogIndexesTimer = new Timer(t => elasticLogManager.DeleteOldLogIndexes(), null, TimeSpan.Zero, TimeSpan.FromDays(1));

            Log.Information("Index service started");
        }

        /// <summary>
        ///     Stops the Index Service.
        ///     Called by the service host when the service is stopped.
        /// </summary>
        public void Stop()
        {
            Log.Information("Index service is stopping");
            bus.Stop();
            Log.Information("Index service stopped");
            Log.CloseAndFlush();
        }
    }
}