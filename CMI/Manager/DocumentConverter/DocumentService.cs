using System;
using Autofac;
using CMI.Contract.DocumentConverter;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Manager.DocumentConverter.Abbyy;
using CMI.Manager.DocumentConverter.Consumers;
using CMI.Manager.DocumentConverter.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;

namespace CMI.Manager.DocumentConverter
{
    public class DocumentService
    {
        private const string serviceName = "DocumentConverter service";

        private ContainerBuilder containerBuilder;
        private IContainer container;

        private IBusControl bus;

        public DocumentService()
        {
            containerBuilder = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        public void Start()
        {
            Log.Information($"{serviceName} is starting");
            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.DocumentConverterService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(BusConstants.DocumentConverterJobInitRequestQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<JobInitConsumer>);
                    ec.UseRetry(BusConfigurator.ConfigureDefaultRetryPolicy);
                });

                cfg.ReceiveEndpoint(BusConstants.DocumentConverterJobEndRequestQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<JobEndConsumer>);
                    ec.UseRetry(BusConfigurator.ConfigureDefaultRetryPolicy);
                });

                cfg.ReceiveEndpoint(BusConstants.DocumentConverterConversionStartRequestQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<ConversionStartConsumer>);
                    ec.UseRetry(BusConfigurator.ConfigureDefaultRetryPolicy);
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });

                cfg.ReceiveEndpoint(BusConstants.DocumentConverterExtractionStartRequestQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<ExtractionStartConsumer>);
                    ec.UseRetry(BusConfigurator.ConfigureDefaultRetryPolicy);
                    BusConfigurator.SetPrefetchCountForEndpoint(ec);
                });

                cfg.ReceiveEndpoint(BusConstants.DocumentConverterSupportedFileTypesRequestQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<SupportedFileTypesConsumer>);
                    ec.UseRetry(retry => retry.Incremental(3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(0)));
                });

                cfg.ReceiveEndpoint(BusConstants.MonitoringDocumentConverterInfoQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<DocumentConverterInfoConsumer>);
                    ec.PrefetchCount = 4;
                });

                cfg.ReceiveEndpoint(BusConstants.MonitoringAbbyyOcrTestQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<AbbyyOcrTestConsumer>);
                    // Do not allow more than 4 concurrent Abbyy calls
                    ec.PrefetchCount = 4;
                }); 
                cfg.UseNewtonsoftJsonSerializer();
            });

            container = containerBuilder.Build();
            bus = container.Resolve<IBusControl>();
            bus.Start();

            // Send event that document converter service is started
            // Push an error message to indicate that the item has failed
            bus.Publish<DocumentConverterServiceStartedEvent>(new
            {
                __TimeToLive = TimeSpan.FromSeconds(30),
                StartTime = DateTime.Now
            });

            Log.Information($"{serviceName} started");
        }

        public void Stop()
        {
            Log.Information($"{serviceName} is stopping");
            bus.Stop();
            if (container.TryResolve<EnginesPool>(out var pool))
                pool.Dispose();

            Log.Information($"{serviceName} stopped");
            Log.CloseAndFlush();
        }
    }
}