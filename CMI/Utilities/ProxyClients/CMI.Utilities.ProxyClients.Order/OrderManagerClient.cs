using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            var client = GetRequestClient<AddToBasketRequest, AddToBasketResponse>();

            var request = new AddToBasketRequest
            {
                UserId = userId,
                IndexSnapshot = indexSnapShot
            };

            var result = await client.Request(request);
            return result.OrderItem;
        }

        public async Task<OrderItem> AddToBasketCustom(string bestand, string ablieferung, string behaeltnisNummer, string archivNummer,
            string aktenzeichen, string dossiertitel, string zeitraumDossier, string userId)
        {
            var client = GetRequestClient<AddToBasketCustomRequest, AddToBasketCustomResponse>();
            var result = await client.Request(new AddToBasketCustomRequest
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

            return result.OrderItem;
        }

        public async Task RemoveFromBasket(int orderItemId, string userId)
        {
            var client = GetRequestClient<RemoveFromBasketRequest, RemoveFromBasketResponse>();
            await client.Request(new RemoveFromBasketRequest {OrderItemId = orderItemId, UserId = userId});
        }

        public async Task UpdateComment(int orderItemId, string comment, string userId)
        {
            var client = GetRequestClient<UpdateCommentRequest, UpdateCommentResponse>();
            await client.Request(new UpdateCommentRequest {OrderItemId = orderItemId, Comment = comment, UserId = userId});
        }

        public async Task UpdateBenutzungskopie(int orderItemId, bool? benutzungskopie)
        {
            var client = GetRequestClient<UpdateBenutzungskopieRequest, UpdateBenutzungskopieResponse>();
            await client.Request(new UpdateBenutzungskopieRequest {OrderItemId = orderItemId, Benutzungskopie = benutzungskopie});
        }

        public async Task UpdateBewilligungsDatum(int orderItemId, DateTime? bewilligungsDatum, string userId)
        {
            var client = GetRequestClient<UpdateBewilligungsDatumRequest, UpdateBewilligungsDatumResponse>();
            await client.Request(new UpdateBewilligungsDatumRequest
            {
                OrderItemId = orderItemId,
                BewilligungsDatum = bewilligungsDatum,
                UserId = userId
            });
        }

        public async Task UpdateReason(int orderItemId, int? reason, bool hasPersonendaten, string userId)
        {
            var client = GetRequestClient<UpdateReasonRequest, UpdateReasonResponse>();
            await client.Request(new UpdateReasonRequest
            {
                OrderItemId = orderItemId,
                Reason = reason,
                HasPersonendaten = hasPersonendaten,
                UserId = userId
            });
        }

        public async Task<IEnumerable<OrderItem>> GetBasket(string userId)
        {
            var client = GetRequestClient<GetBasketRequest, GetBasketResponse>();
            var result = await client.Request(new GetBasketRequest {UserId = userId});
            return result.OrderItems;
        }

        public async Task UpdateOrderDetail(UpdateOrderDetailData updateData)
        {
            var client =
                GetRequestClient<UpdateOrderDetailRequest, UpdateOrderDetailResponse>(BusConstants.OrderManagerUpdateOrderDetailRequestQueue);
            await client.Request(new UpdateOrderDetailRequest {UpdateData = updateData});
        }

        public async Task CreateOrderFromBasket(OrderCreationRequest ocr)
        {
            var client = GetRequestClient<OrderCreationRequest, CreateOrderFromBasketResponse>();
            await client.Request(ocr);
        }

        public async Task<IEnumerable<Ordering>> GetOrderings(string userId)
        {
            var client = GetRequestClient<GetOrderingsRequest, GetOrderingsResponse>();
            var result = await client.Request(new GetOrderingsRequest {UserId = userId});
            return result.Orderings;
        }

        public async Task<Ordering> GetOrdering(int orderingId)
        {
            var client = GetRequestClient<GetOrderingRequest, GetOrderingResponse>(BusConstants.OrderManagerGetOrderingRequestQueue);
            var result = await client.Request(new GetOrderingRequest {OrderingId = orderingId});
            return result.Ordering;
        }

        public async Task<OrderItem[]> FindOrderItems(int[] orderItemIds)
        {
            var client = GetRequestClient<FindOrderItemsRequest, FindOrderItemsResponse>(BusConstants.OrderManagerFindOrderItemsRequestQueue);
            var result = await client.Request(new FindOrderItemsRequest {OrderItemIds = orderItemIds});
            return result.OrderItems;
        }

        public async Task<bool> IsUniqueVeInBasket(int veId, string userId)
        {
            var client = GetRequestClient<IsUniqueVeInBasketRequest, IsUniqueVeInBasketResponse>();
            var result = await client.Request(new IsUniqueVeInBasketRequest {VeId = veId, UserId = userId});
            return result.IsUniqueVeInBasket;
        }

        public async Task<DigipoolEntry[]> GetDigipool(int numberOfEntries)
        {
            var client = GetRequestClient<GetDigipoolRequest, GetDigipoolResponse>();
            var result = await client.Request(new GetDigipoolRequest {NumberOfEntries = numberOfEntries});
            return result.GetDigipool;
        }

        public async Task UpdateDigipool(List<int> orderItemIds, int? digitalisierungsKategorie, DateTime? terminDigitalisierung)
        {
            var client = GetRequestClient<UpdateDigipoolRequest, UpdateDigipoolResponse>();
            await client.Request(new UpdateDigipoolRequest
            {
                OrderItemIds = orderItemIds,
                DigitalisierungsKategorie = digitalisierungsKategorie,
                TerminDigitalisierung = terminDigitalisierung
            });
        }

        public async Task<IEnumerable<StatusHistory>> GetStatusHistoryForOrderItem(int orderItemId)
        {
            var client = GetRequestClient<GetStatusHistoryForOrderItemRequest, GetStatusHistoryForOrderItemResponse>(BusConstants
                .OrderManagerGetStatusHistoryForOrderItemRequestQueue);
            var result = await client.Request(new GetStatusHistoryForOrderItemRequest {OrderItemId = orderItemId});
            return result.StatusHistory;
        }

        public async Task<List<Bestellhistorie>> GetOrderingHistoryForVe(int veId)
        {
            var client = GetRequestClient<FindOrderingHistoryForVeRequest, FindOrderingHistoryForVeResponse>(BusConstants
                .OrderManagerFindOrderingHistoryForVeRequestQueue);
            var result = await client.Request(new FindOrderingHistoryForVeRequest {VeId = veId});
            return result.History;
        }

        public async Task EntscheidFreigabeHinterlegen(string currentUserId, List<int> orderItemIds, ApproveStatus entscheid,
            DateTime? datumBewilligung, string interneBemerkung)
        {
            var client =
                GetRequestClient<EntscheidFreigabeHinterlegenRequest, EntscheidFreigabeHinterlegenResponse>(BusConstants
                    .EntscheidFreigabeHinterlegenRequestQueue);
            var entscheidFreigabeHinterlegenRequest = new EntscheidFreigabeHinterlegenRequest
            {
                UserId = currentUserId,
                OrderItemIds = orderItemIds,
                DatumBewilligung = datumBewilligung,
                Entscheid = entscheid,
                InterneBemerkung = interneBemerkung
            };

            await client.Request(entscheidFreigabeHinterlegenRequest);
        }

        public async Task AushebungsauftraegeDrucken(string currentUserId, List<int> orderItemIds)
        {
            var client =
                GetRequestClient<AushebungsauftraegeDruckenRequest, AushebungsauftraegeDruckenResponse>(BusConstants
                    .AushebungsauftraegeDruckenRequestQueue);
            var r = new AushebungsauftraegeDruckenRequest
            {
                UserId = currentUserId,
                OrderItemIds = orderItemIds
            };

            await client.Request(r);
        }

        public async Task EntscheidGesuchHinterlegen(string currentUserId, List<int> orderItemIds, EntscheidGesuch entscheid,
            DateTime datumEntscheid, string interneBemerkung)
        {
            var client =
                GetRequestClient<EntscheidGesuchHinterlegenRequest, EntscheidGesuchHinterlegenResponse>(BusConstants
                    .EntscheidGesuchHinterlegenRequestQueue);
            var entscheidGesuchHinterlegenRequest = new EntscheidGesuchHinterlegenRequest
            {
                UserId = currentUserId,
                OrderItemIds = orderItemIds,
                DatumEntscheid = datumEntscheid,
                Entscheid = entscheid,
                InterneBemerkung = interneBemerkung
            };

            await client.Request(entscheidGesuchHinterlegenRequest);
        }

        public async Task InVorlageExportieren(string currentUserId, List<int> orderItemIds, Vorlage vorlage, string sprache)
        {
            var client = GetRequestClient<InVorlageExportierenRequest, InVorlageExportierenResponse>(BusConstants.InVorlageExportierenRequestQueue);
            var request = new InVorlageExportierenRequest
            {
                OrderItemIds = orderItemIds,
                Sprache = sprache,
                CurrentUserId = currentUserId,
                Vorlage = vorlage
            };

            await client.Request(request);
        }

        public async Task ZumReponierenBereit(string currentUserId, List<int> orderItemsId)
        {
            var client =
                GetRequestClient<SetStatusZumReponierenBereitRequest, SetStatusZumReponierenBereitResponse>(BusConstants.ReponierenRequestQueue);
            var request = new SetStatusZumReponierenBereitRequest
            {
                OrderItemIds = orderItemsId,
                UserId = currentUserId
            };

            await client.Request(request);
        }

        public async Task Abschliessen(string currentUserId, List<int> orderItemIds)
        {
            var client = GetRequestClient<AbschliessenRequest, AbschliessenResponse>(BusConstants.AbschliessenRequestQueue);
            var request = new AbschliessenRequest
            {
                OrderItemIds = orderItemIds,
                CurrentUserId = currentUserId
            };

            await client.Request(request);
        }

        public async Task Abbrechen(string currentUserId, List<int> orderItemIds, Abbruchgrund abbruchgrund, string bemerkungZumDossier,
            string interneBemerkung)
        {
            var client = GetRequestClient<AbbrechenRequest, AbbrechenResponse>(BusConstants.AbbrechenRequestQueue);
            var request = new AbbrechenRequest
            {
                OrderItemIds = orderItemIds,
                CurrentUserId = currentUserId,
                Abbruchgrund = abbruchgrund,
                InterneBemerkung = interneBemerkung,
                BemerkungZumDossier = bemerkungZumDossier
            };

            await client.Request(request);
        }

        public async Task Zuruecksetzen(string currentUserId, List<int> orderItemIds)
        {
            var client = GetRequestClient<ZuruecksetzenRequest, ZuruecksetzenResponse>(BusConstants.ZuruecksetzenRequestQueue);
            var request = new ZuruecksetzenRequest
            {
                OrderItemIds = orderItemIds,
                CurrentUserId = currentUserId
            };

            await client.Request(request);
        }

        public async Task AuftraegeAusleihen(string currentUserId, List<int> orderItemIds)
        {
            var client = GetRequestClient<AuftraegeAusleihenRequest, AuftraegeAusleihenResponse>(BusConstants.AuftraegeAusleihenRequestQueue);
            var request = new AuftraegeAusleihenRequest
            {
                OrderItemIds = orderItemIds,
                CurrentUserId = currentUserId
            };

            await client.Request(request);
        }

        public async Task DigitalisierungAusloesen(string currentUserId, OrderingIndexSnapshot[] snapshots, int artDerArbeit)
        {
            var client =
                GetRequestClient<DigitalisierungAusloesenRequest, DigitalisierungAusloesenResponse>(BusConstants
                    .DigitalisierungAusloesenRequestQueue);
            var request = new DigitalisierungAusloesenRequest
            {
                Snapshots = snapshots,
                CurrentUserId = currentUserId,
                ArtDerArbeit = artDerArbeit
            };

            await client.Request(request);
        }

        public async Task MarkOrderAsFaulted(int orderItemId)
        {
            var client = GetRequestClient<MarkOrderAsFaultedRequest, MarkOrderAsFaultedResponse>(BusConstants.OrderManagerMarkOrderAsFaultedQueue);
            var request = new MarkOrderAsFaultedRequest {OrderItemId = orderItemId};

            await client.Request(request);
        }

        public async Task ResetAufbereitungsfehler(List<int> orderItemIds)
        {
            var client =
                GetRequestClient<ResetAufbereitungsfehlerRequest, ResetAufbereitungsfehlerResponse>(BusConstants.OrderManagerResetFaultedOrdersQueue);
            await client.Request(new ResetAufbereitungsfehlerRequest
            {
                OrderItemIds = orderItemIds
            });
        }

        public async Task<MahnungVersendenResponse> MahnungVersenden(List<int> orderItemIds, string language, int gewaehlteMahnungAnzahl,
            string userId)
        {
            var mahnungRequest = new MahnungVersendenRequest
                {OrderItemIds = orderItemIds, Language = language, GewaehlteMahnungAnzahl = gewaehlteMahnungAnzahl, UserId = userId};
            var client = GetRequestClient<MahnungVersendenRequest, MahnungVersendenResponse>(BusConstants.OrderManagerMahnungVersendenRequestQueue,
                200);
            return await client.Request(mahnungRequest);
        }

        private IRequestClient<T1, T2> GetRequestClient<T1, T2>(string queueEndpoint = "", int requestTimeOutInSeconds = 0)
            where T1 : class where T2 : class
        {
            var serviceUrl = string.IsNullOrEmpty(queueEndpoint)
                ? string.Format(BusConstants.OrderManagagerRequestBase, typeof(T1).Name)
                : queueEndpoint;
#if DEBUG
            var requestTimeout = TimeSpan.FromSeconds(120);
            var timeToLive = TimeSpan.FromSeconds(120);
#else
            var requestTimeout = TimeSpan.FromSeconds(10);
            var timeToLive = TimeSpan.FromSeconds(20);
#endif

            if (requestTimeOutInSeconds > 0)
            {
                requestTimeout = TimeSpan.FromSeconds(requestTimeOutInSeconds);
            }

            return new MessageRequestClient<T1, T2>(bus, new Uri(bus.Address, serviceUrl), requestTimeout, timeToLive);
        }
    }
}