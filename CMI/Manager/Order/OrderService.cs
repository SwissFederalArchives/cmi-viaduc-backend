using System;
using System.Reflection;
using Autofac;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Manager.Order.Consumers;
using CMI.Manager.Order.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;
using RetryConfigurationExtensions = MassTransit.RetryConfigurationExtensions;

namespace CMI.Manager.Order
{
    public class OrderService
    {
        private readonly ContainerBuilder containerBuilder;
        private RecalcTermineListener recalcTermineListener;
        private IBusControl bus;

        public OrderService()
        {
            // Configure IoC Container
            containerBuilder = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        /// <summary>
        ///     Starts the Order Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public void Start()
        {
            Log.Information("Order service is starting");

            // Configure Bus
            var helper = new ParameterBusHelper();
            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.OrderService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(AddToBasketRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<AddToBasketRequest>>); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerFindOrderItemsRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<FindOrderItemsRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerGetStatusHistoryForOrderItemRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<GetStatusHistoryForOrderItemConsumer>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(GetPrimaerdatenReportRecordsRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetPrimaerdatenReportRecordsRequest>>); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerFindOrderingHistoryForVeRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<FindOrderHistoryForVeConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerGetOrderingRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<GetOrderingRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerUpdateOrderDetailRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<UpdateOrderDetailRequestConsumer>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(AddToBasketCustomRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<AddToBasketCustomRequest>>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(RemoveFromBasketRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<RemoveFromBasketRequest>>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(UpdateCommentRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<UpdateCommentRequest>>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(UpdateBenutzungskopieRequest)),
                    ec =>
                    {
                        ec.Consumer(ctx.Resolve<IConsumer<UpdateBenutzungskopieRequest>>);
                    });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(UpdateBewilligungsDatumRequest)),
                    ec =>
                    {
                        ec.Consumer(ctx.Resolve<IConsumer<UpdateBewilligungsDatumRequest>>);
                    });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(UpdateReasonRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<UpdateReasonRequest>>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(OrderCreationRequest)),
                    ec => { ec.Consumer(ctx.Resolve<CreateOrderFromBasketRequestConsumer>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(GetBasketRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetBasketRequest>>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(GetOrderingsRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetOrderingsRequest>>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(IsUniqueVeInBasketRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<IsUniqueVeInBasketRequest>>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(GetDigipoolRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<GetDigipoolRequest>>); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerSetStatusAushebungBereitRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<SetStatusAushebungBereitConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerSetStatusDigitalisierungExternRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<SetStatusDigitalisierungExternConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerSetStatusDigitalisierungAbgebrochenRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<SetStatusDigitalisierungAbgebrochenConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerSetStatusZumReponierenBereitRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<SetStatusZumReponierenBereitConsumer>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(UpdateDigipoolRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<UpdateDigipoolRequest>>); });

                cfg.ReceiveEndpoint(BusConstants.EntscheidFreigabeHinterlegenRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<EntscheidFreigabeHinterlegenRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.EntscheidGesuchHinterlegenRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<EntscheidGesuchHinterlegenRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.InVorlageExportierenRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<InVorlageExportierenRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.ReponierenRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<SetStatusZumReponierenBereitConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.AbschliessenRequestQueue, ec => { ec.Consumer(ctx.Resolve<AbschliessenRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.AbbrechenRequestQueue, ec => { ec.Consumer(ctx.Resolve<AbbrechenRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.ZuruecksetzenRequestQueue, ec => { ec.Consumer(ctx.Resolve<ZuruecksetzenRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.AuftraegeAusleihenRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<AuftraegeAusleihenRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.AushebungsauftraegeDruckenRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<AushebungsauftraegeDruckenRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.RecalcIndivTokens, ec => { ec.Consumer(ctx.Resolve<RecalcIndivTokensConsumer>); });
                cfg.ReceiveEndpoint(BusConstants.DigitalisierungAusloesenRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<DigitalisierungAusloesenRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.DigitalisierungsAuftragErledigtEvent, ec =>
                {
                    ec.Consumer(ctx.Resolve<DigitalisierungsAuftragErledigtConsumer>);
                    ec.UseRetry((Action<IRetryConfigurator>) BusConfigurator.ConfigureDefaultRetryPolicy);
                });
                cfg.ReceiveEndpoint(BusConstants.OrderManagerArchiveRecordUpdatedEventQueue, ec =>
                {
                    ec.Consumer(ctx.Resolve<ArchiveRecordUpdatedConsumer>);
                    ((MassTransit.IPipeConfigurator<ConsumeContext>) ec).UseRetry((Action<IRetryConfigurator>) BusConfigurator.ConfigureDefaultRetryPolicy);
                });
                cfg.ReceiveEndpoint(BusConstants.DigitalisierungsAuftragErledigtEventError,
                    ec => { ec.Consumer(ctx.Resolve<DigitalisierungsAuftragErledigtErrorConsumer>); });
                cfg.ReceiveEndpoint(BusConstants.BenutzungskopieAuftragErledigtEvent, ec =>
                {
                    ec.Consumer(ctx.Resolve<BenutzungskopieAuftragErledigtConsumer>);
                    // Wenn Vecteur meldet, dass Auftrag erledigt ist, kann es sein, dass die Daten eventuell noch nicht in den SFTP hochgeladen wurden.
                    // Der Consumer löst in diesem Fall eine Exception aus. Durch den Retry versuchen wir es noch ein paar mal
#if DEBUG
                    ec.UseRetry(retryPolicy => retryPolicy.Interval(5, TimeSpan.FromSeconds(2)));
#else
                    ec.UseRetry(retryPolicy => retryPolicy.Interval(10, TimeSpan.FromMinutes(30)));
#endif
                });
                cfg.ReceiveEndpoint(BusConstants.BenutzungskopieAuftragErledigtEventError,
                    ec => { ec.Consumer(ctx.Resolve<BenutzungskopieAuftragErledigtErrorConsumer>); });
                cfg.ReceiveEndpoint(BusConstants.OrderManagerAssetReadyEventQueue, ec => { ec.Consumer(ctx.Resolve<AssetReadyConsumer>); });
                cfg.ReceiveEndpoint(BusConstants.OrderManagerMarkOrderAsFaultedQueue,
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<MarkOrderAsFaultedRequest>>); });
                cfg.ReceiveEndpoint(BusConstants.OrderManagerResetFaultedOrdersQueue,
                    ec =>
                    {
                        ec.Consumer(ctx.Resolve<IConsumer<ResetAufbereitungsfehlerRequest>>);
                    });
                cfg.ReceiveEndpoint(BusConstants.OrderManagerMahnungVersendenRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<MahnungVersendenRequestConsumer>); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerErinnerungVersendenRequestQueue,
                    ec => { ec.Consumer(ctx.Resolve<ErinnerungVersendenRequestConsumer>); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(UpdateOrderItemRequest)),
                    ec => { ec.Consumer(ctx.Resolve<IConsumer<UpdateOrderItemRequest>>); });

                cfg.UseNewtonsoftJsonSerializer();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });

            containerBuilder.Register(CreateFindArchiveRecordRequestClient);
            containerBuilder.Register(CreateRegisterPrepareAssetClient);

            var container = containerBuilder.Build();

            bus = container.Resolve<IBusControl>();
            bus.Start();
            
            recalcTermineListener = new RecalcTermineListener(container.Resolve<IOrderDataAccess>());
            recalcTermineListener.Start();

            Log.Information("Order service started");
        }


        /// <summary>
        ///     Stops the Order Service.
        ///     Called by the service host when the service is stopped.
        /// </summary>
        public void Stop()
        {
            Log.Information("Order service is stopping");
            recalcTermineListener.Stop();
            bus.Stop();
            Log.Information("Order service stopped");
            Log.CloseAndFlush();
        }

        private IRequestClient<FindArchiveRecordRequest> CreateFindArchiveRecordRequestClient(IComponentContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(1);
            var uri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.IndexManagerFindArchiveRecordMessageQueue);
            return bus.CreateRequestClient<FindArchiveRecordRequest>(uri, requestTimeout);
        }

        private IRequestClient<PrepareAssetRequest> CreateRegisterPrepareAssetClient(IComponentContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(1);
            var uri = new Uri(new Uri(BusConfigurator.Uri), BusConstants.WebApiPrepareAssetRequestQueue);
            return bus.CreateRequestClient<PrepareAssetRequest>(uri, requestTimeout);
        }
    }
}