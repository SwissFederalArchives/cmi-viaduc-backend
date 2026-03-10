using CMI.Access.Sql.Viaduc.EF;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Viaduc.Consumer;
using CMI.Manager.Viaduc.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace CMI.Manager.Viaduc
{
    public class ViaducService
    {
        private IBusControl bus;

        public void Start()
        {
            LogConfigurator.ConfigureForService();

            Log.Information("Viaduc service is starting");

            var services = ContainerConfigurator.Configure();

            BusConfigurator.ConfigureBusModern(services, MonitoredServices.ViaducService, AddConsumers, (context, cfg) =>
            {
                cfg.ReceiveEndpoint(BusConstants.ReadUserInformationQueue,
                    e => { e.ConfigureConsumer<ReadUserInformationConsumer>(context); });
                cfg.ReceiveEndpoint(BusConstants.ReadStammdatenQueue,
                    e => { e.ConfigureConsumer<ReadStammdatenConsumer>(context); });

                // CollectionManager Methods
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetAllCollectionsRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<GetAllCollectionsRequest, GetAllCollectionsResponse, ICollectionManager>>(context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetActiveCollectionsRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<GetActiveCollectionsRequest, GetActiveCollectionsResponse, ICollectionManager>>(
                        context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetCollectionsHeaderRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<GetCollectionsHeaderRequest, GetCollectionsHeaderResponse, ICollectionManager>>(
                        context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetCollectionRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<GetCollectionRequest, GetCollectionResponse, ICollectionManager>>(context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(InsertOrUpdateCollectionRequest)),
                    e => e
                        .ConfigureConsumer<SimpleConsumer<InsertOrUpdateCollectionRequest, InsertOrUpdateCollectionResponse, ICollectionManager>>(
                            context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(DeleteCollectionRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<DeleteCollectionRequest, DeleteCollectionResponse, ICollectionManager>>(context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(BatchDeleteCollectionRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<BatchDeleteCollectionRequest, BatchDeleteCollectionResponse, ICollectionManager>>(
                        context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetPossibleParentsRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<GetPossibleParentsRequest, GetPossibleParentsResponse, ICollectionManager>>(context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetImageRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<GetImageRequest, GetImageResponse, ICollectionManager>>(context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetCollectionItemResultRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<GetCollectionItemResultRequest, GetCollectionItemResultResponse, ICollectionManager>>(
                        context));

                // ManuelleKorrekturManager Methods
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetManuelleKorrekturRequest)),
                    e =>
                        e.ConfigureConsumer<SimpleConsumer<GetManuelleKorrekturRequest, GetManuelleKorrekturResponse, IManuelleKorrekturManager>>(
                            context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(DeleteManuelleKorrekturRequest)),
                    e => e
                        .ConfigureConsumer<
                            SimpleConsumer<DeleteManuelleKorrekturRequest, DeleteManuelleKorrekturResponse, IManuelleKorrekturManager>>(context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(BatchDeleteManuelleKorrekturRequest)),
                    e => e
                        .ConfigureConsumer<
                            SimpleConsumer<BatchDeleteManuelleKorrekturRequest, BatchDeleteManuelleKorrekturResponse, IManuelleKorrekturManager>>(
                            context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(BatchAddManuelleKorrekturRequest)),
                    e => e
                        .ConfigureConsumer<
                            SimpleConsumer<BatchAddManuelleKorrekturRequest, BatchAddManuelleKorrekturResponse, IManuelleKorrekturManager>>(
                            context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(InsertOrUpdateManuelleKorrekturRequest)),
                    e => e
                        .ConfigureConsumer<
                            SimpleConsumer<InsertOrUpdateManuelleKorrekturRequest, InsertOrUpdateManuelleKorrekturResponse,
                                IManuelleKorrekturManager>>(context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(PublizierenManuelleKorrekturRequest)),
                    e => e
                        .ConfigureConsumer<
                            SimpleConsumer<PublizierenManuelleKorrekturRequest, PublizierenManuelleKorrekturResponse, IManuelleKorrekturManager>>(
                            context));

                // Synchronisation
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetLogDataRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<GetLogDataRequest, GetLogDataResponse, ISynchronisationManager>>(context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetSyncDataRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<GetSyncDataRequest, GetSyncDataResponse, ISynchronisationManager>>(context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(BatchAddSyncActionsRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<BatchAddSyncActionsRequest, BatchAddSyncActionsResponse, ISynchronisationManager>>(
                        context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetSyncNumberPerHourRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<GetSyncNumberPerHourRequest, GetSyncNumberPerHourResponse, ISynchronisationManager>>(
                        context));

                // Sync Action Methods
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(GetPendingMutationsRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<GetPendingMutationsRequest, GetPendingMutationsResponse, IViaducDataProvider>>(
                        context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(UpdateMutationStatusRequest)),
                    e => e.ConfigureConsumer<SimpleConsumer<UpdateMutationStatusRequest, UpdateMutationStatusResponse, IViaducDataProvider>>(
                        context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(BulkUpdateMutationStatusRequest)),
                    e => e
                        .ConfigureConsumer<
                            SimpleConsumer<BulkUpdateMutationStatusRequest, BulkUpdateMutationStatusResponse, IViaducDataProvider>>(context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(ResetFailedSyncOperationsRequest)),
                    e => e
                        .ConfigureConsumer<
                            SimpleConsumer<ResetFailedSyncOperationsRequest, ResetFailedSyncOperationsResponse, IViaducDataProvider>>(context));
                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(InsertSyncActionRequest)),
                    e => e.ConfigureConsumer<InsertSyncActionRequestConsumer>(context));

                cfg.ReceiveEndpoint(string.Format(BusConstants.ViaducManagerRequestBase, nameof(DeleteOldSyncActionRequest)),
                    e => e.ConfigureConsumer<DeleteOldSyncActionRequestConsumer>(context));


                cfg.UseNewtonsoftJsonSerializer();
                var helper = new ParameterBusHelper();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });


            var provider = services.BuildServiceProvider();
            bus = provider.GetRequiredService<IBusControl>();

            bus.Start();

            Log.Information("Viaduc service started");
        }

        private void AddConsumers(IBusRegistrationConfigurator x)
        {
            // registers all IConsumer implementations in this assembly
            x.AddConsumers(Assembly.GetExecutingAssembly());

            // register all the generic implementations
            // CollectionManager
            x.AddConsumer<SimpleConsumer<GetAllCollectionsRequest, GetAllCollectionsResponse, ICollectionManager>>();
            x.AddConsumer<SimpleConsumer<GetActiveCollectionsRequest, GetActiveCollectionsResponse, ICollectionManager>>();
            x.AddConsumer<SimpleConsumer<GetCollectionsHeaderRequest, GetCollectionsHeaderResponse, ICollectionManager>>();
            x.AddConsumer<SimpleConsumer<GetCollectionRequest, GetCollectionResponse, ICollectionManager>>();
            x.AddConsumer<SimpleConsumer<InsertOrUpdateCollectionRequest, InsertOrUpdateCollectionResponse, ICollectionManager>>();
            x.AddConsumer<SimpleConsumer<DeleteCollectionRequest, DeleteCollectionResponse, ICollectionManager>>();
            x.AddConsumer<SimpleConsumer<BatchDeleteCollectionRequest, BatchDeleteCollectionResponse, ICollectionManager>>();
            x.AddConsumer<SimpleConsumer<GetPossibleParentsRequest, GetPossibleParentsResponse, ICollectionManager>>();
            x.AddConsumer<SimpleConsumer<GetImageRequest, GetImageResponse, ICollectionManager>>();
            x.AddConsumer<SimpleConsumer<GetCollectionItemResultRequest, GetCollectionItemResultResponse, ICollectionManager>>();

            // ManuelleKorrekturManager
            x.AddConsumer<SimpleConsumer<GetManuelleKorrekturRequest, GetManuelleKorrekturResponse, IManuelleKorrekturManager>>();
            x.AddConsumer<SimpleConsumer<DeleteManuelleKorrekturRequest, DeleteManuelleKorrekturResponse, IManuelleKorrekturManager>>();
            x.AddConsumer<SimpleConsumer<BatchDeleteManuelleKorrekturRequest, BatchDeleteManuelleKorrekturResponse, IManuelleKorrekturManager>>();
            x.AddConsumer<SimpleConsumer<BatchAddManuelleKorrekturRequest, BatchAddManuelleKorrekturResponse, IManuelleKorrekturManager>>();
            x.AddConsumer<SimpleConsumer<InsertOrUpdateManuelleKorrekturRequest, InsertOrUpdateManuelleKorrekturResponse, IManuelleKorrekturManager>>();
            x.AddConsumer<SimpleConsumer<PublizierenManuelleKorrekturRequest, PublizierenManuelleKorrekturResponse, IManuelleKorrekturManager>>();

            // Sync Action
            x.AddConsumer<SimpleConsumer<GetLogDataRequest, GetLogDataResponse, ISynchronisationManager>>();
            x.AddConsumer<SimpleConsumer<GetSyncDataRequest, GetSyncDataResponse, ISynchronisationManager>>();
            x.AddConsumer<SimpleConsumer<BatchAddSyncActionsRequest, BatchAddSyncActionsResponse, ISynchronisationManager>>();
            x.AddConsumer<SimpleConsumer<GetSyncNumberPerHourRequest, GetSyncNumberPerHourResponse, ISynchronisationManager>>();
            x.AddConsumer<SimpleConsumer<GetPendingMutationsRequest, GetPendingMutationsResponse, IViaducDataProvider>>();
            x.AddConsumer<SimpleConsumer<UpdateMutationStatusRequest, UpdateMutationStatusResponse, IViaducDataProvider>>();
            x.AddConsumer<SimpleConsumer<BulkUpdateMutationStatusRequest, BulkUpdateMutationStatusResponse, IViaducDataProvider>>();
            x.AddConsumer<SimpleConsumer<ResetFailedSyncOperationsRequest, ResetFailedSyncOperationsResponse, IViaducDataProvider>>();
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