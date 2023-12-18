using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;

namespace CMI.Utilities.ProxyClients.Order
{
    public class OrderManagerClient : IPublicOrder
    {
        private readonly IBus bus;

        public OrderManagerClient(IBus bus)
        {
            this.bus = bus;
        }

        public async Task<OrderItem> AddToBasket(OrderingIndexSnapshot indexSnapShot, string userId)
        {
            var client = GetRequestClient<AddToBasketRequest>();

            var request = new AddToBasketRequest
            {
                UserId = userId,
                IndexSnapshot = indexSnapShot
            };

            var result = await client.GetResponse<AddToBasketResponse>(request);
            return result.Message.OrderItem;
        }

        public async Task<OrderItem> AddToBasketCustom(string bestand, string ablieferung, string behaeltnisNummer, string archivNummer,
            string aktenzeichen, string dossiertitel, string zeitraumDossier, string userId)
        {
            var client = GetRequestClient<AddToBasketCustomRequest>();
            var result = await client.GetResponse<AddToBasketCustomResponse>(new AddToBasketCustomRequest
            {
                Bestand = bestand,
                Ablieferung = ablieferung,
                BehaeltnisNummer = behaeltnisNummer,
                ArchivNummer = archivNummer,
                Aktenzeichen = aktenzeichen,
                Dossiertitel = dossiertitel,
                ZeitraumDossier = zeitraumDossier,
                UserId = userId
            });

            return result.Message.OrderItem;
        }

        public async Task RemoveFromBasket(int orderItemId, string userId)
        {
            var client = GetRequestClient<RemoveFromBasketRequest>();
            await client.GetResponse<RemoveFromBasketResponse>(new RemoveFromBasketRequest {OrderItemId = orderItemId, UserId = userId});
        }

        public async Task UpdateComment(int orderItemId, string comment, string userId)
        {
            var client = GetRequestClient<UpdateCommentRequest>();
            await client.GetResponse<UpdateCommentResponse>(new UpdateCommentRequest {OrderItemId = orderItemId, Comment = comment, UserId = userId});
        }

        public async Task UpdateBenutzungskopie(int orderItemId, bool? benutzungskopie)
        {
            var client = GetRequestClient<UpdateBenutzungskopieRequest>();
            await client.GetResponse<UpdateBenutzungskopieResponse>(new UpdateBenutzungskopieRequest {OrderItemId = orderItemId, Benutzungskopie = benutzungskopie});
        }

        public async Task UpdateBewilligungsDatum(int orderItemId, DateTime? bewilligungsDatum, string userId)
        {
            var client = GetRequestClient<UpdateBewilligungsDatumRequest>();
            await client.GetResponse<UpdateBewilligungsDatumResponse>(new UpdateBewilligungsDatumRequest
            {
                OrderItemId = orderItemId,
                BewilligungsDatum = bewilligungsDatum,
                UserId = userId
            });
        }

        public async Task UpdateReason(int orderItemId, int? reason, bool hasPersonendaten, string userId)
        {
            var client = GetRequestClient<UpdateReasonRequest>();
            await client.GetResponse<UpdateReasonResponse>(new UpdateReasonRequest
            {
                OrderItemId = orderItemId,
                Reason = reason,
                HasPersonendaten = hasPersonendaten,
                UserId = userId
            });
        }

        public async Task<IEnumerable<OrderItem>> GetBasket(string userId)
        {
            var client = GetRequestClient<GetBasketRequest>();
            var result = await client.GetResponse<GetBasketResponse>(new GetBasketRequest {UserId = userId});
            return result.Message.OrderItems;
        }

        public async Task UpdateOrderDetail(UpdateOrderDetailData updateData)
        {
            var client = GetRequestClient<UpdateOrderDetailRequest>(BusConstants.OrderManagerUpdateOrderDetailRequestQueue);
            await client.GetResponse<UpdateOrderDetailResponse>(new UpdateOrderDetailRequest {UpdateData = updateData});
        }

        public async Task CreateOrderFromBasket(OrderCreationRequest ocr)
        {
            var client = GetRequestClient<OrderCreationRequest>();
            await client.GetResponse<CreateOrderFromBasketResponse>(ocr);
        }

        public async Task<IEnumerable<Ordering>> GetOrderings(string userId)
        {
            var client = GetRequestClient<GetOrderingsRequest>();
            var result = await client.GetResponse<GetOrderingsResponse>(new GetOrderingsRequest {UserId = userId});
            return result.Message.Orderings;
        }

        public async Task<Ordering> GetOrdering(int orderingId)
        {
            var client = GetRequestClient<GetOrderingRequest>(BusConstants.OrderManagerGetOrderingRequestQueue);
            var result = await client.GetResponse<GetOrderingResponse>(new GetOrderingRequest {OrderingId = orderingId});
            return result.Message.Ordering;
        }

        public async Task<OrderItem[]> FindOrderItems(int[] orderItemIds)
        {
            var client = GetRequestClient<FindOrderItemsRequest>(BusConstants.OrderManagerFindOrderItemsRequestQueue);
            var result = await client.GetResponse<FindOrderItemsResponse>(new FindOrderItemsRequest {OrderItemIds = orderItemIds});
            return result.Message.OrderItems;
        }

        public async Task<bool> IsUniqueVeInBasket(int veId, string userId)
        {
            var client = GetRequestClient<IsUniqueVeInBasketRequest>();
            var result = await client.GetResponse<IsUniqueVeInBasketResponse>(new IsUniqueVeInBasketRequest {VeId = veId, UserId = userId});
            return result.Message.IsUniqueVeInBasket;
        }

        public async Task<DigipoolEntry[]> GetDigipool(int numberOfEntries)
        {
            var client = GetRequestClient<GetDigipoolRequest>();
            var result = await client.GetResponse<GetDigipoolResponse>(new GetDigipoolRequest {NumberOfEntries = numberOfEntries});
            return result.Message.GetDigipool;
        }

        public async Task UpdateDigipool(List<int> orderItemIds, int? digitalisierungsKategorie, DateTime? terminDigitalisierung)
        {
            var client = GetRequestClient<UpdateDigipoolRequest>();
            await client.GetResponse<UpdateDigipoolResponse>(new UpdateDigipoolRequest
            {
                OrderItemIds = orderItemIds,
                DigitalisierungsKategorie = digitalisierungsKategorie,
                TerminDigitalisierung = terminDigitalisierung
            });
        }

        public async Task<List<DownloadLogItem>> GetDownloadLogReportRecords(LogDataFilter filter)
        {
            var client = GetRequestClient<GetDownloadLogReportRecordsRequest>(string.Empty, 3600);
            var request = new GetDownloadLogReportRecordsRequest { Filter = filter };
            var result = await client.GetResponse<GetDownloadLogReportRecordsResponse>(request);
             
            return result.Message.Items;
        }

        public async Task<IEnumerable<StatusHistory>> GetStatusHistoryForOrderItem(int orderItemId)
        {
            var client = GetRequestClient<GetStatusHistoryForOrderItemRequest>(BusConstants
                .OrderManagerGetStatusHistoryForOrderItemRequestQueue);
            var result = await client.GetResponse<GetStatusHistoryForOrderItemResponse>(new GetStatusHistoryForOrderItemRequest { OrderItemId = orderItemId });
            return result.Message.StatusHistory;
        }

        public async Task<List<Bestellhistorie>> GetOrderingHistoryForVe(int veId)
        {
            var client = GetRequestClient<FindOrderingHistoryForVeRequest>(BusConstants
                .OrderManagerFindOrderingHistoryForVeRequestQueue);
            var result = await client.GetResponse<FindOrderingHistoryForVeResponse>(new FindOrderingHistoryForVeRequest {VeId = veId});
            return result.Message.History;
        }

        public async Task<List<PrimaerdatenAufbereitungItem>> GetPrimaerdatenReportRecords(LogDataFilter filter)
        {
            var client = GetRequestClient<GetPrimaerdatenReportRecordsRequest>( string.Empty, 3600);
            var request = new GetPrimaerdatenReportRecordsRequest {Filter = filter};
            var result = await client.GetResponse<GetPrimaerdatenReportRecordsResponse>(request);

            return result.Message.Items;
        }

        public async Task EntscheidFreigabeHinterlegen(string currentUserId, List<int> orderItemIds, ApproveStatus entscheid,
            DateTime? datumBewilligung, string interneBemerkung)
        {
            var client =
                GetRequestClient<EntscheidFreigabeHinterlegenRequest>(BusConstants
                    .EntscheidFreigabeHinterlegenRequestQueue);
            var entscheidFreigabeHinterlegenRequest = new EntscheidFreigabeHinterlegenRequest
            {
                UserId = currentUserId,
                OrderItemIds = orderItemIds,
                DatumBewilligung = datumBewilligung,
                Entscheid = entscheid,
                InterneBemerkung = interneBemerkung
            };

            await client.GetResponse<EntscheidFreigabeHinterlegenResponse>(entscheidFreigabeHinterlegenRequest);
        }

        public async Task AushebungsauftraegeDrucken(string currentUserId, List<int> orderItemIds)
        {
            var client =
                GetRequestClient<AushebungsauftraegeDruckenRequest>(BusConstants
                    .AushebungsauftraegeDruckenRequestQueue);
            var r = new AushebungsauftraegeDruckenRequest
            {
                UserId = currentUserId,
                OrderItemIds = orderItemIds
            };

            await client.GetResponse<AushebungsauftraegeDruckenResponse>(r);
        }

        public async Task EntscheidGesuchHinterlegen(string currentUserId, List<int> orderItemIds, EntscheidGesuch entscheid,
            DateTime datumEntscheid, string interneBemerkung)
        {
            var client =
                GetRequestClient<EntscheidGesuchHinterlegenRequest>(BusConstants
                    .EntscheidGesuchHinterlegenRequestQueue);
            var entscheidGesuchHinterlegenRequest = new EntscheidGesuchHinterlegenRequest
            {
                UserId = currentUserId,
                OrderItemIds = orderItemIds,
                DatumEntscheid = datumEntscheid,
                Entscheid = entscheid,
                InterneBemerkung = interneBemerkung
            };

            await client.GetResponse<EntscheidGesuchHinterlegenResponse>(entscheidGesuchHinterlegenRequest);
        }

        public async Task InVorlageExportieren(string currentUserId, List<int> orderItemIds, Vorlage vorlage, string sprache)
        {
            var client = GetRequestClient<InVorlageExportierenRequest>(BusConstants.InVorlageExportierenRequestQueue);
            var request = new InVorlageExportierenRequest
            {
                OrderItemIds = orderItemIds,
                Sprache = sprache,
                CurrentUserId = currentUserId,
                Vorlage = vorlage
            };

            await client.GetResponse<InVorlageExportierenResponse>(request);
        }

        public async Task ZumReponierenBereit(string currentUserId, List<int> orderItemsId)
        {
            var client =
                GetRequestClient<SetStatusZumReponierenBereitRequest>(BusConstants.ReponierenRequestQueue);
            var request = new SetStatusZumReponierenBereitRequest
            {
                OrderItemIds = orderItemsId,
                UserId = currentUserId
            };

            await client.GetResponse<SetStatusZumReponierenBereitResponse>(request);
        }

        public async Task Abschliessen(string currentUserId, List<int> orderItemIds)
        {
            var client = GetRequestClient<AbschliessenRequest>(BusConstants.AbschliessenRequestQueue);
            var request = new AbschliessenRequest
            {
                OrderItemIds = orderItemIds,
                CurrentUserId = currentUserId
            };

            await client.GetResponse<AbschliessenResponse>(request);
        }

        public async Task Abbrechen(string currentUserId, List<int> orderItemIds, Abbruchgrund abbruchgrund, string bemerkungZumDossier,
            string interneBemerkung)
        {
            var client = GetRequestClient<AbbrechenRequest>(BusConstants.AbbrechenRequestQueue);
            var request = new AbbrechenRequest
            {
                OrderItemIds = orderItemIds,
                CurrentUserId = currentUserId,
                Abbruchgrund = abbruchgrund,
                InterneBemerkung = interneBemerkung,
                BemerkungZumDossier = bemerkungZumDossier
            };

            await client.GetResponse<AbbrechenResponse>(request);
        }

        public async Task Zuruecksetzen(string currentUserId, List<int> orderItemIds)
        {
            var client = GetRequestClient<ZuruecksetzenRequest>(BusConstants.ZuruecksetzenRequestQueue);
            var request = new ZuruecksetzenRequest
            {
                OrderItemIds = orderItemIds,
                CurrentUserId = currentUserId
            };

            await client.GetResponse<ZuruecksetzenResponse>(request);
        }

        public async Task AuftraegeAusleihen(string currentUserId, List<int> orderItemIds)
        {
            var client = GetRequestClient<AuftraegeAusleihenRequest>(BusConstants.AuftraegeAusleihenRequestQueue);
            var request = new AuftraegeAusleihenRequest
            {
                OrderItemIds = orderItemIds,
                CurrentUserId = currentUserId
            };

            await client.GetResponse<AuftraegeAusleihenResponse>(request);
        }

        public async Task DigitalisierungAusloesen(string currentUserId, OrderingIndexSnapshot[] snapshots, int artDerArbeit)
        {
            var client =
                GetRequestClient<DigitalisierungAusloesenRequest>(BusConstants
                    .DigitalisierungAusloesenRequestQueue);
            var request = new DigitalisierungAusloesenRequest
            {
                Snapshots = snapshots,
                CurrentUserId = currentUserId,
                ArtDerArbeit = artDerArbeit
            };

            await client.GetResponse<DigitalisierungAusloesenResponse>(request);
        }

        public async Task MarkOrderAsFaulted(int orderItemId)
        {
            var client = GetRequestClient<MarkOrderAsFaultedRequest>(BusConstants.OrderManagerMarkOrderAsFaultedQueue);
            var request = new MarkOrderAsFaultedRequest {OrderItemId = orderItemId};

            await client.GetResponse<MarkOrderAsFaultedResponse>(request);
        }

        public async Task ResetAufbereitungsfehler(List<int> orderItemIds)
        {
            var client = GetRequestClient<ResetAufbereitungsfehlerRequest>(BusConstants.OrderManagerResetFaultedOrdersQueue);
            await client.GetResponse<ResetAufbereitungsfehlerResponse>(new ResetAufbereitungsfehlerRequest
            {
                OrderItemIds = orderItemIds
            });
        }

        public async Task<MahnungVersendenResponse> MahnungVersenden(List<int> orderItemIds, string language, int gewaehlteMahnungAnzahl,
            string userId)
        {
            var mahnungRequest = new MahnungVersendenRequest
                {OrderItemIds = orderItemIds, Language = language, GewaehlteMahnungAnzahl = gewaehlteMahnungAnzahl, UserId = userId};
            var client = GetRequestClient<MahnungVersendenRequest>(BusConstants.OrderManagerMahnungVersendenRequestQueue,
                200);
            return (await client.GetResponse<MahnungVersendenResponse>(mahnungRequest)).Message;
        }

        public async Task<ErinnerungVersendenResponse> ErinnerungVersenden(List<int> orderItemIds, string userId)
        {
            var erinnerungRequest = new ErinnerungVersendenRequest { OrderItemIds = orderItemIds, UserId = userId };
            var client = GetRequestClient<ErinnerungVersendenRequest>(BusConstants.OrderManagerErinnerungVersendenRequestQueue,
                200);
            return (await client.GetResponse<ErinnerungVersendenResponse>(erinnerungRequest)).Message;
        }

        public async Task<int> UpdateOrderItem(OrderItem item)
        {
            var client = GetRequestClient<UpdateOrderItemRequest>();

            var request = new UpdateOrderItemRequest
            {
                OrderItem = item
            };

            return (await client.GetResponse<UpdateOrderItemResponse>(request)).Message.OrderItemId;
        }

        private IRequestClient<T1> GetRequestClient<T1>(string queueEndpoint = "", int requestTimeOutInSeconds = 0) where T1 : class
        {
            var serviceUrl = string.IsNullOrEmpty(queueEndpoint)
                ? string.Format(BusConstants.OrderManagagerRequestBase, typeof(T1).Name)
                : queueEndpoint;
            
            #if DEBUG
                var requestTimeout = TimeSpan.FromSeconds(120);
            #else
                var requestTimeout = TimeSpan.FromSeconds(10);
            #endif

            if (requestTimeOutInSeconds > 0)
            {
                requestTimeout = TimeSpan.FromSeconds(requestTimeOutInSeconds);
            }

            return bus.CreateRequestClient<T1>(new Uri(bus.Address, serviceUrl), requestTimeout);
        }
    }
}