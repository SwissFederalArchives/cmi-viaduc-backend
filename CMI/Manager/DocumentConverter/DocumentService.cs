using System;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Manager.DocumentConverter.Abbyy;
using CMI.Manager.DocumentConverter.Consumers;
using CMI.Manager.DocumentConverter.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using GreenPipes;
using MassTransit;
using Ninject;
using Serilog;

namespace CMI.Manager.DocumentConverter
{
    public class DocumentService
    {
        private const string serviceName = "DocumentConverter service";

        private readonly StandardKernel kernel;

        private IBusControl bus;

        public DocumentService()
        {
            kernel = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        public void Start()
        {
            Log.Information($"{serviceName} is starting");
            bus = BusConfigurator.ConfigureBus(MonitoredServices.DocumentConverterService, (cfg, host) =>
            {
                kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
                kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();

                cfg.ReceiveEndpoint(BusConstants.DocumentConverterJobInitRequestQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<JobInitConsumer>());
                    ec.UseRetry(BusConfigurator.ConfigureDefaultRetryPolicy);
                });

                cfg.ReceiveEndpoint(BusConstants.DocumentConverterConversionStartRequestQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<ConversionStartConsumer>());
                    ec.UseRetry(BusConfigurator.ConfigureDefaultRetryPolicy);
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });

                cfg.ReceiveEndpoint(BusConstants.DocumentConverterExtractionStartRequestQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<ExtractionStartConsumer>());
                    ec.UseRetry(BusConfigurator.ConfigureDefaultRetryPolicy);
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });

                cfg.ReceiveEndpoint(BusConstants.DocumentConverterSupportedFileTypesRequestQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<SupportedFileTypesConsumer>());
                    ec.UseRetry(retry => retry.Incremental(3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(0)));
                });

                cfg.ReceiveEndpoint(BusConstants.MonitoringDocumentConverterInfoQueue,
                    ec => { ec.Consumer(() => kernel.Get<DocumentConverterInfoConsumer>()); });

                cfg.UseSerilog();
            });

            bus.Start();

            Log.Information($"{serviceName} started");
        }

        public void Stop()
        {
            Log.Information($"{serviceName} is stopping");
            bus.Stop();
            var pool = kernel.TryGet<EnginesPool>();
            pool?.Dispose();
            Log.Information($"{serviceName} stopped");
            Log.CloseAndFlush();
        }
    }
}