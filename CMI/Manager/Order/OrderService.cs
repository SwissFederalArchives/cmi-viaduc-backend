using System;
using System.Reflection;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Manager.Order.Consumers;
using CMI.Manager.Order.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using GreenPipes;
using MassTransit;
using Ninject;
using Ninject.Activation;
using Serilog;

namespace CMI.Manager.Order
{
    public class OrderService
    {
        private readonly StandardKernel kernel;
        private readonly RecalcTermineListener recalcTermineListener;
        private IBusControl bus;

        public OrderService()
        {
            // Configure IoC Container
            kernel = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();

            recalcTermineListener = new RecalcTermineListener(kernel.Get<IOrderDataAccess>());
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
            bus = BusConfigurator.ConfigureBus(MonitoredServices.OrderService, (cfg, host) =>
            {
                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(AddToBasketRequest)),
                    ec => { ec.Consumer(() => kernel.Get<SimpleConsumer<AddToBasketRequest, AddToBasketResponse, IPublicOrder>>()); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerFindOrderItemsRequestQueue,
                    ec => { ec.Consumer(() => new FindOrderItemsRequestConsumer(kernel.Get<IPublicOrder>())); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerGetStatusHistoryForOrderItemRequestQueue,
                    ec => { ec.Consumer(() => new GetStatusHistoryForOrderItemConsumer(kernel.Get<IPublicOrder>())); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerFindOrderingHistoryForVeRequestQueue,
                    ec => { ec.Consumer(() => new FindOrderHistoryForVeConsumer(kernel.Get<IPublicOrder>())); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerGetOrderingRequestQueue,
                    ec => { ec.Consumer(() => new GetOrderingRequestConsumer(kernel.Get<IPublicOrder>())); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerUpdateOrderDetailRequestQueue,
                    ec => { ec.Consumer(() => new UpdateOrderDetailRequestConsumer(kernel.Get<IPublicOrder>())); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(AddToBasketCustomRequest)),
                    ec => { ec.Consumer(() => kernel.Get<SimpleConsumer<AddToBasketCustomRequest, AddToBasketCustomResponse, IPublicOrder>>()); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(RemoveFromBasketRequest)),
                    ec => { ec.Consumer(() => kernel.Get<SimpleConsumer<RemoveFromBasketRequest, RemoveFromBasketResponse, IPublicOrder>>()); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(UpdateCommentRequest)),
                    ec => { ec.Consumer(() => kernel.Get<SimpleConsumer<UpdateCommentRequest, UpdateCommentResponse, IPublicOrder>>()); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(UpdateBenutzungskopieRequest)),
                    ec =>
                    {
                        ec.Consumer(() => kernel.Get<SimpleConsumer<UpdateBenutzungskopieRequest, UpdateBenutzungskopieResponse, IPublicOrder>>());
                    });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(UpdateBewilligungsDatumRequest)),
                    ec =>
                    {
                        ec.Consumer(() =>
                            kernel.Get<SimpleConsumer<UpdateBewilligungsDatumRequest, UpdateBewilligungsDatumResponse, IPublicOrder>>());
                    });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(UpdateReasonRequest)),
                    ec => { ec.Consumer(() => kernel.Get<SimpleConsumer<UpdateReasonRequest, UpdateReasonResponse, IPublicOrder>>()); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(OrderCreationRequest)),
                    ec => { ec.Consumer(() => kernel.Get<CreateOrderFromBasketRequestConsumer>()); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(GetBasketRequest)),
                    ec => { ec.Consumer(() => kernel.Get<SimpleConsumer<GetBasketRequest, GetBasketResponse, IPublicOrder>>()); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(GetOrderingsRequest)),
                    ec => { ec.Consumer(() => kernel.Get<SimpleConsumer<GetOrderingsRequest, GetOrderingsResponse, IPublicOrder>>()); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(IsUniqueVeInBasketRequest)),
                    ec => { ec.Consumer(() => kernel.Get<SimpleConsumer<IsUniqueVeInBasketRequest, IsUniqueVeInBasketResponse, IPublicOrder>>()); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(GetDigipoolRequest)),
                    ec => { ec.Consumer(() => kernel.Get<SimpleConsumer<GetDigipoolRequest, GetDigipoolResponse, IPublicOrder>>()); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerSetStatusAushebungBereitRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<SetStatusAushebungBereitConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerSetStatusDigitalisierungExternRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<SetStatusDigitalisierungExternConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerSetStatusDigitalisierungAbgebrochenRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<SetStatusDigitalisierungAbgebrochenConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.OrderManagerSetStatusZumReponierenBereitRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<SetStatusZumReponierenBereitConsumer>()); });

                cfg.ReceiveEndpoint(string.Format(BusConstants.OrderManagagerRequestBase, nameof(UpdateDigipoolRequest)),
                    ec => { ec.Consumer(() => kernel.Get<SimpleConsumer<UpdateDigipoolRequest, UpdateDigipoolResponse, IPublicOrder>>()); });

                cfg.ReceiveEndpoint(BusConstants.EntscheidFreigabeHinterlegenRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<EntscheidFreigabeHinterlegenRequestConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.EntscheidGesuchHinterlegenRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<EntscheidGesuchHinterlegenRequestConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.InVorlageExportierenRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<InVorlageExportierenRequestConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.ReponierenRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<SetStatusZumReponierenBereitConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.AbschliessenRequestQueue, ec => { ec.Consumer(() => kernel.Get<AbschliessenRequestConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.AbbrechenRequestQueue, ec => { ec.Consumer(() => kernel.Get<AbbrechenRequestConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.ZuruecksetzenRequestQueue, ec => { ec.Consumer(() => kernel.Get<ZuruecksetzenRequestConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.AuftraegeAusleihenRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<AuftraegeAusleihenRequestConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.AushebungsauftraegeDruckenRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<AushebungsauftraegeDruckenRequestConsumer>()); });

                cfg.ReceiveEndpoint(BusConstants.RecalcIndivTokens, ec => { ec.Consumer(() => kernel.Get<RecalcIndivTokensConsumer>()); });
                cfg.ReceiveEndpoint(BusConstants.DigitalisierungAusloesenRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<DigitalisierungAusloesenRequestConsumer>()); });
                cfg.ReceiveEndpoint(BusConstants.DigitalisierungsAuftragErledigtEvent, ec =>
                {
                    ec.Consumer(() => kernel.Get<DigitalisierungsAuftragErledigtConsumer>());
                    ec.UseRetry(BusConfigurator.ConfigureDefaultRetryPolicy);
                });
                cfg.ReceiveEndpoint(BusConstants.OrderManagerArchiveRecordUpdatedEventQueue, ec =>
                {
                    ec.Consumer(() => kernel.Get<ArchiveRecordUpdatedConsumer>());
                    ec.UseRetry(BusConfigurator.ConfigureDefaultRetryPolicy);
                });
                cfg.ReceiveEndpoint(BusConstants.DigitalisierungsAuftragErledigtEventError,
                    ec => { ec.Consumer(() => kernel.Get<DigitalisierungsAuftragErledigtErrorConsumer>()); });
                cfg.ReceiveEndpoint(BusConstants.BenutzungskopieAuftragErledigtEvent, ec =>
                {
                    ec.Consumer(() => kernel.Get<BenutzungskopieAuftragErledigtConsumer>());
                    // Wenn Vecteur meldet, dass Auftrag erledigt ist, kann es sein, dass die Daten eventuell noch nicht in den SFTP hochgeladen wurden.
                    // Der Consumer löst in diesem Fall eine Exception aus. Durch den Retry versuchen wir es noch ein paar mal
#if DEBUG
                    ec.UseRetry(retryPolicy => retryPolicy.Interval(5, TimeSpan.FromSeconds(2)));
#else
                    ec.UseRetry(retryPolicy => retryPolicy.Interval(10, TimeSpan.FromMinutes(30)));
#endif
                });
                cfg.ReceiveEndpoint(BusConstants.BenutzungskopieAuftragErledigtEventError,
                    ec => { ec.Consumer(() => kernel.Get<BenutzungskopieAuftragErledigtErrorConsumer>()); });
                cfg.ReceiveEndpoint(BusConstants.OrderManagerAssetReadyEventQueue, ec => { ec.Consumer(() => kernel.Get<AssetReadyConsumer>()); });
                cfg.ReceiveEndpoint(BusConstants.OrderManagerMarkOrderAsFaultedQueue,
                    ec => { ec.Consumer(() => kernel.Get<SimpleConsumer<MarkOrderAsFaultedRequest, MarkOrderAsFaultedResponse, OrderManager>>()); });
                cfg.ReceiveEndpoint(BusConstants.OrderManagerResetFaultedOrdersQueue,
                    ec =>
                    {
                        ec.Consumer(() =>
                            kernel.Get<SimpleConsumer<ResetAufbereitungsfehlerRequest, ResetAufbereitungsfehlerResponse, OrderManager>>());
                    });
                cfg.ReceiveEndpoint(BusConstants.OrderManagerMahnungVersendenRequestQueue,
                    ec => { ec.Consumer(() => kernel.Get<MahnungVersendenRequestConsumer>()); });

                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg, host);
                cfg.UseSerilog();
            });

            // Add the bus instance to the IoC container
            kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IBusControl>().ToMethod(context => bus).InSingletonScope();
            kernel.Bind<IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>>()
                .ToMethod(CreateFindArchiveRecordRequestClient);


            bus.Start();
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

        private IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse> CreateFindArchiveRecordRequestClient(IContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(1);

            var client =
                new MessageRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>(bus,
                    new Uri(new Uri(BusConfigurator.Uri), BusConstants.IndexManagerFindArchiveRecordMessageQueue), requestTimeout, null,
                    ChangeResponseAddress);

            return client;
        }

        private void ChangeResponseAddress(SendContext<FindArchiveRecordRequest> context)
        {
            if (!string.IsNullOrEmpty(BusConfigurator.ResponseUri) && BusConfigurator.ResponseUri != BusConfigurator.Uri)
            {
                context.ResponseAddress = new Uri(context.ResponseAddress.OriginalString.ToLower()
                    .Replace(BusConfigurator.Uri.ToLower(), BusConfigurator.ResponseUri.ToLower()));
            }
        }
    }
}