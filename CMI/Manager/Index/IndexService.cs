using System;
using System.Reflection;
using System.Threading;
using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Index.Consumer;
using CMI.Manager.Index.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using GreenPipes;
using MassTransit;
using Serilog;

namespace CMI.Manager.Index
{
    /// <summary>
    ///     The index service is configuring the bus and the <see cref="IndexManager" />
    /// </summary>
    public class IndexService
    {
        private readonly ContainerBuilder containerBuilder;
        private IBusControl bus;

        // ReSharper disable once NotAccessedField.Local 
        private Timer deleteOldLogIndexesTimer;


        public IndexService()
        {
            // Configure IoC Container
            containerBuilder = ContainerConfigurator.Configure();
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
            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.IndexService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(BusConstants.IndexManagerUpdateArchiveRecordMessageQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<UpdateArchiveRecordConsumer>);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.IndexManagerRemoveArchiveRecordMessageQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<RemoveArchiveRecordConsumer>);
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });
                cfg.ReceiveEndpoint(BusConstants.IndexManagerFindArchiveRecordMessageQueue,
                    ec => { ec.Consumer(ctx.Resolve<FindArchiveRecordConsumer>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.IndexManagagerRequestBase, nameof(GetArchiveRecordsForPackageRequest)),
                    ec =>
                    {
                        ec.Consumer(ctx.Resolve<IConsumer<GetArchiveRecordsForPackageRequest>>);
                    });

                cfg.ReceiveEndpoint(BusConstants.IndexManagerUpdateIndivTokensMessageQueue,
                    ec => { ec.Consumer(ctx.Resolve<UpdateIndivTokensConsumer>); });
                cfg.ReceiveEndpoint(BusConstants.MonitoringElasticSearchTestQueue,
                    ec => { ec.Consumer(ctx.Resolve<TestElasticSearchRequestConsumer>); });
                cfg.ReceiveEndpoint(BusConstants.IndexManagerGetElasticLogRecordsRequestQueue,
                    ec => {  ec.Consumer(ctx.Resolve<IConsumer<GetElasticLogRecordsRequest>>); });

                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });

            var container = containerBuilder.Build();
            bus = container.Resolve<IBusControl>();
            bus.Start();
            
            var elasticLogManager = container.Resolve<IElasticLogManager>();
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