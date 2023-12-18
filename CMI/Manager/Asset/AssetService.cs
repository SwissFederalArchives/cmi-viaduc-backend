using System;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using CMI.Contract.DocumentConverter;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Engine.Asset.PreProcess;
using CMI.Manager.Asset.Consumers;
using CMI.Manager.Asset.Infrasctructure;
using CMI.Manager.Asset.Properties;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Quartz;
using Serilog;

namespace CMI.Manager.Asset
{
    public class AssetService
    {
        private readonly ContainerBuilder builder;
        private IContainer container;

        private IBusControl bus;
        private IScheduler scheduler;

        public AssetService()
        {
            // Configure IoC Container
            builder = ContainerConfigurator.CreateContainerBuilder();
            LogConfigurator.ConfigureForService();
        }

        /// <summary>
        ///     Starts the Asset Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public async Task Start()
        {
            Log.Information("Asset service is starting");

            EnsurePasswordSeedIsConfigured();
            var helper = new ParameterBusHelper();

            // Configure Bus
            BusConfigurator.ConfigureBus(builder, MonitoredServices.AssetService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(BusConstants.AssetManagerExtractFulltextMessageQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<ExtractFulltextPackageConsumer>);
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });

                cfg.ReceiveEndpoint(BusConstants.AssetManagerTransformAssetMessageQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<TransformPackageConsumer>);
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });

                cfg.ReceiveEndpoint(BusConstants.AssetManagerPrepareForRecognition, ec =>
                {
                    ec.Consumer(ctx.Resolve<PrepareForRecognitionConsumer>);
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });

                cfg.ReceiveEndpoint(BusConstants.AssetManagerRecognitionPostProcessing, ec =>
                {
                    ec.Consumer(ctx.Resolve<RecognitionPostProcessingConsumer>);
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });

                cfg.ReceiveEndpoint(BusConstants.AssetManagerPrepareForTransformation, ec =>
                {
                    ec.Consumer(ctx.Resolve<PrepareForTransformationConsumer>);
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });

                cfg.ReceiveEndpoint(BusConstants.WebApiDownloadAssetRequestQueue, ec => { ec.Consumer(ctx.Resolve<DownloadAssetConsumer>); });
                cfg.ReceiveEndpoint(BusConstants.WebApiGetAssetStatusRequestQueue, ec => { ec.Consumer(ctx.Resolve<GetAssetStatusConsumer>); });
                cfg.ReceiveEndpoint(BusConstants.WebApiPrepareAssetRequestQueue, ec => { ec.Consumer(ctx.Resolve<PrepareAssetConsumer>); });
                cfg.ReceiveEndpoint(BusConstants.AssetManagerAssetReadyEventQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<AssetReadyConsumer>);
                    // Retry or we have the situation where the job is not marked as terminated in the DB.
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });

                cfg.ReceiveEndpoint(BusConstants.AssetManagerSchdeduleForPackageSyncMessageQueue, ec => { ec.Consumer(ctx.Resolve<ScheduleForPackageSyncConsumer>); });
                cfg.ReceiveEndpoint(BusConstants.AssetManagerUpdatePrimaerdatenAuftragStatusMessageQueue, ec => { ec.Consumer(ctx.Resolve<UpdatePrimaerdatenAuftragStatusConsumer>); });
                cfg.UseNewtonsoftJsonSerializer();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
                helper.SubscribeAllSettingsInAssembly(Assembly.GetAssembly(typeof(AssetPreparationEngine)), cfg);
            });

            builder.Register(CreateDoesExistInCacheRequestClient);
            builder.Register(CreateJobInitRequestClient);
            builder.Register(CreateJobEndRequestClient);
            builder.Register(CreateSupportedFileTypesRequestClient);
            builder.Register(CreateDocumentConversionRequestClient);
            builder.Register(CreateDocumentExtractionRequestClient);
            builder.Register(CreateFindArchiveRecordRequestClient);
            
            container = builder.Build();
            scheduler = await SchedulerConfigurator.Configure(container);

            bus = container.Resolve<IBusControl>();
            bus.Start();

            // Start the timer
            Log.Verbose("Starting scheduler");
            await scheduler.Start();

            Log.Information("Asset service started");
        }

        /// <summary>
        ///     Wirft eine Exception, wenn der PasswordSeed den (nicht ersetzten) Platzhalter für UrbanCode
        ///     enthält. Das wäre dann der Fall, wenn beim Deployment dieser Platzhalter nicht ordnungsgemäss ersetzt würde.
        /// </summary>
        private static void EnsurePasswordSeedIsConfigured()
        {
            if (string.IsNullOrWhiteSpace(Settings.Default.PasswordSeed) ||
                Settings.Default.PasswordSeed.StartsWith("@@") && Settings.Default.PasswordSeed.EndsWith("@@"))
            {
                Log.Error("Password Seed is not properly configured. It seems to contain a placeholder for UrbanCode or is empty.");
                throw new Exception("PasswordSeed is not configured.");
            }
        }

        public IRequestClient<DoesExistInCacheRequest> CreateDoesExistInCacheRequestClient(IComponentContext context)
        {
            var requestTimeout = TimeSpan.FromHours(1);

            return bus.CreateRequestClient<DoesExistInCacheRequest>(new Uri(new Uri(BusConfigurator.Uri), BusConstants.CacheDoesExistRequestQueue), requestTimeout);
        }

        public IRequestClient<JobInitRequest> CreateJobInitRequestClient(IComponentContext context)
        {
            var requestTimeout = TimeSpan.FromHours(1);
            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterJobInitRequestQueue);

            return bus.CreateRequestClient<JobInitRequest>(busUri, requestTimeout);
        }

        public IRequestClient<JobEndRequest> CreateJobEndRequestClient(IComponentContext context)
        {
            var requestTimeout = TimeSpan.FromHours(1);
            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterJobEndRequestQueue);

            return bus.CreateRequestClient<JobEndRequest>(busUri, requestTimeout);
        }

        public IRequestClient<SupportedFileTypesRequest> CreateSupportedFileTypesRequestClient(IComponentContext context)
        {
            var requestTimeout = TimeSpan.FromHours(1);
            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterSupportedFileTypesRequestQueue);

            return bus.CreateRequestClient<SupportedFileTypesRequest>(busUri, requestTimeout);
        }

        public IRequestClient<ConversionStartRequest> CreateDocumentConversionRequestClient(IComponentContext context)
        {
            // Very large files could take a very long time to convert
            var requestTimeout = TimeSpan.FromHours(96);
            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterConversionStartRequestQueue);

            return bus.CreateRequestClient<ConversionStartRequest>(busUri, requestTimeout);
        }

        public IRequestClient<ExtractionStartRequest> CreateDocumentExtractionRequestClient(IComponentContext context)
        {
            // Ocr of a large pdf can take some time
            var requestTimeout = TimeSpan.FromHours(96);
            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterExtractionStartRequestQueue);

            return bus.CreateRequestClient<ExtractionStartRequest>(busUri, requestTimeout);
        }

        public IRequestClient<FindArchiveRecordRequest> CreateFindArchiveRecordRequestClient(IComponentContext context)
        {
            var requestTimeout = TimeSpan.FromHours(1);
            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.IndexManagerFindArchiveRecordMessageQueue);

            return bus.CreateRequestClient<FindArchiveRecordRequest>(busUri, requestTimeout);
        }


        /// <summary>
        ///     Stops the Asset Service.
        ///     Called by the service host when the service is stopped.
        /// </summary>
        public void Stop()
        {
            Log.Information("Asset service is stopping");
            bus.Stop();
            Log.Information("Asset service stopped");
            Log.CloseAndFlush();
        }
    }
}