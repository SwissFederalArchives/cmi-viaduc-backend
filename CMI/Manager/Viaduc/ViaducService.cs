using System;
using System.Reflection;
using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Viaduc.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;

namespace CMI.Manager.Viaduc
{
    public class ViaducService
    {
        private IBusControl bus;

        public void Start()
        {
            LogConfigurator.ConfigureForService();

            Log.Information("Viaduc service is starting");

            var containerBuilder = ContainerConfigurator.Configure();

            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.ViaducService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(BusConstants.ReadUserInformationQueue,
                    ec => { ec.Consumer(ctx.Resolve<ReadUserInformationConsumer>); }
                );
                cfg.ReceiveEndpoint(BusConstants.ReadStammdatenQueue,
                    ec => { ec.Consumer(ctx.Resolve<ReadStammdatenConsumer>); }
                );
                // CollectionManager Methods
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetAllCollectionsRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetAllCollectionsRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetActiveCollectionsRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetActiveCollectionsRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetCollectionsHeaderRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetCollectionsHeaderRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetCollectionRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetCollectionRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(InsertOrUpdateCollectionRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<InsertOrUpdateCollectionRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(DeleteCollectionRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<DeleteCollectionRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(BatchDeleteCollectionRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<BatchDeleteCollectionRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetPossibleParentsRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetPossibleParentsRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetImageRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetImageRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetCollectionItemResultRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetCollectionItemResultRequest>>); });
                
                // ManuelleKorrekturManager Methods
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetManuelleKorrekturRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetManuelleKorrekturRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(DeleteManuelleKorrekturRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<DeleteManuelleKorrekturRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(BatchDeleteManuelleKorrekturRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<BatchDeleteManuelleKorrekturRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(BatchAddManuelleKorrekturRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<BatchAddManuelleKorrekturRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(InsertOrUpdateManuelleKorrekturRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<InsertOrUpdateManuelleKorrekturRequest>>); });
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(PublizierenManuelleKorrekturRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<PublizierenManuelleKorrekturRequest>>); });
                cfg.UseNewtonsoftJsonSerializer();
                var helper = new ParameterBusHelper();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });
            var container = containerBuilder.Build();
            bus = container.Resolve<IBusControl>();
            bus.Start();

            Log.Information("Viaduc service started");
        }

        public void Stop()
        {
            Log.Information("Viaduc service is stopping.");
            bus.Stop();
            Log.Information("Viaduc service has stopped.");
            Log.CloseAndFlush();
        }
    }
}