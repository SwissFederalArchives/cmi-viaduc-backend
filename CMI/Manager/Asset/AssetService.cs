using System;
using System.Reflection;
using System.Threading.Tasks;
using CMI.Contract.DocumentConverter;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Asset.Consumers;
using CMI.Manager.Asset.Infrasctructure;
using CMI.Manager.Asset.Properties;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using GreenPipes;
using MassTransit;
using Ninject;
using Ninject.Activation;
using Quartz;
using Serilog;

namespace CMI.Manager.Asset
{
    public class AssetService
    {
        private readonly StandardKernel kernel;
        private IBusControl bus;
        private IScheduler scheduler;

        public AssetService()
        {
            // Configure IoC Container
            kernel = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        /// <summary>
        ///     Starts the Asset Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public async Task Start()
        {
            Log.Information("Asset service is starting");
            scheduler = await SchedulerConfigurator.Configure(kernel);

            EnsurePasswordSeedIsConfigured();


            // Configure Bus
            var helper = new ParameterBusHelper();
            bus = BusConfigurator.ConfigureBus(MonitoredServices.AssetService, (cfg, host) =>
            {
                cfg.ReceiveEndpoint(BusConstants.AssetManagerExtractFulltextMessageQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<ExtractFulltextPackageConsumer>());
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });

                cfg.ReceiveEndpoint(BusConstants.AssetManagerTransformAssetMessageQueue,
                    ec =>
                    {
                        ec.Consumer(() => kernel.Get<TransformPackageConsumer>());
                        BusConfigurator.SetPrefetchCountForEndpoint(ec);
                    });

                cfg.ReceiveEndpoint(BusConstants.WebApiDownloadAssetRequestQueue, ec => { ec.Consumer(() => kernel.Get<DownloadAssetConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.WebApiGetAssetStatusRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<GetAssetStatusConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.WebApiPrepareAssetRequestQueue, ec => { ec.Consumer(() => kernel.Get<PrepareAssetConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.AssetManagerAssetReadyEventQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<AssetReadyConsumer>());
                    // Retry or we have the situation where the job is not marked as terminated in the DB.
                    ec.UseRetry(retryPolicy =>
                        retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5)));
                });

                cfg.ReceiveEndpoint(BusConstants.MonitoringAbbyyOcrTestQueue, ec => { ec.Consumer(() => kernel.Get<AbbyyOcrTestConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.AssetManagerSchdeduleForPackageSyncMessageQueue,
                    ec => { ec.Consumer(() => kernel.Get<ScheduleForPackageSyncConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.AssetManagerUpdatePrimaerdatenAuftragStatusMessageQueue,
                    ec => { ec.Consumer(() => kernel.Get<UpdatePrimaerdatenAuftragStatusConsumer>()); });

                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg, host);
                cfg.UseSerilog();
            });

            // Add the bus instance to the IoC container
            kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse>>().ToMethod(CreateDoesExistInCacheRequestClient);
            kernel.Bind<IRequestClient<JobInitRequest, JobInitResult>>().ToMethod(CreateJobInitRequestClient);
            kernel.Bind<IRequestClient<SupportedFileTypesRequest, SupportedFileTypesResponse>>().ToMethod(CreateSupportedFileTypesRequestClient);
            kernel.Bind<IRequestClient<ConversionStartRequest, ConversionStartResult>>().ToMethod(CreateDocumentConversionRequestClient);
            kernel.Bind<IRequestClient<ExtractionStartRequest, ExtractionStartResult>>().ToMethod(CreateDocumentExtractionRequestClient);
            kernel.Bind<IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>>().ToMethod(CreateFindArchiveRecordRequestClient);

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

        public IRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse> CreateDoesExistInCacheRequestClient(IContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var client =
                new MessageRequestClient<DoesExistInCacheRequest, DoesExistInCacheResponse>(bus,
                    new Uri(new Uri(BusConfigurator.Uri), BusConstants.CacheDoesExistRequestQueue), requestTimeout);

            return client;
        }

        public IRequestClient<JobInitRequest, JobInitResult> CreateJobInitRequestClient(IContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterJobInitRequestQueue);
            var client = new MessageRequestClient<JobInitRequest, JobInitResult>(bus, busUri, requestTimeout);

            return client;
        }

        public IRequestClient<SupportedFileTypesRequest, SupportedFileTypesResponse> CreateSupportedFileTypesRequestClient(IContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterSupportedFileTypesRequestQueue);
            var client = new MessageRequestClient<SupportedFileTypesRequest, SupportedFileTypesResponse>(bus, busUri, requestTimeout);

            return client;
        }

        public IRequestClient<ConversionStartRequest, ConversionStartResult> CreateDocumentConversionRequestClient(IContext context)
        {
            // Very large files could take a very long time to convert
            var requestTimeout = TimeSpan.FromHours(96);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterConversionStartRequestQueue);
            var client = new MessageRequestClient<ConversionStartRequest, ConversionStartResult>(bus, busUri, requestTimeout);

            return client;
        }

        public IRequestClient<ExtractionStartRequest, ExtractionStartResult> CreateDocumentExtractionRequestClient(IContext context)
        {
            // Ocr of a large pdf can take some time
            var requestTimeout = TimeSpan.FromHours(12);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.DocumentConverterExtractionStartRequestQueue);
            var client = new MessageRequestClient<ExtractionStartRequest, ExtractionStartResult>(bus, busUri, requestTimeout);

            return client;
        }

        public IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse> CreateFindArchiveRecordRequestClient(IContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var busUri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.IndexManagerFindArchiveRecordMessageQueue);
            var client = new MessageRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>(bus, busUri, requestTimeout);

            return client;
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