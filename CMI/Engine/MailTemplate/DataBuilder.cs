using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;

namespace CMI.Engine.MailTemplate
{
    public class DataBuilder : IDataBuilder
    {
        private readonly IBus bus;
        private dynamic expando;

        // Required constructor for dependency injection
        public DataBuilder(IBus bus) : this(bus, new ExpandoObject())
        {
        }

        public DataBuilder(IBus bus, dynamic expando)
        {
            this.bus = bus;
            Stammdaten.Bus = bus;
            this.expando = expando;
            this.expando.Global = new Global();
        }

        public IDataBuilder AddUser(string userId)
        {
            expando.User = GetPerson(userId);
            return this;
        }

        public IDataBuilder AddBesteller(string bestellerId)
        {
            expando.Besteller = GetPerson(bestellerId);
            return this;
        }

        public IDataBuilder AddBestellung(Ordering ordering)
        {
            expando.Bestellung = new Bestellung(ordering);
            return this;
        }

        public IDataBuilder AddVe(string archiveRecordId)
        {
            expando.Ve = GetVe(archiveRecordId);
            return this;
        }

        public IDataBuilder AddVeList(IEnumerable<string> archiveRecordIdList)
        {
            var veList = new List<InElasticIndexierteVe>();
            foreach (var archiveRecordId in archiveRecordIdList)
            {
                veList.Add(GetVe(archiveRecordId));
            }

            AddVeList(veList);
            return this;
        }

        public IDataBuilder AddVeList(List<InElasticIndexierteVe> veList)
        {
            expando.VeList = veList;
            expando.HatMehrereVe = veList.Count > 1;
            return this;
        }

        /// <param name="sprachCode">Z.B. de</param>
        public IDataBuilder AddSprache(string sprachCode)
        {
            expando.Sprachen = new[] {new Sprache(sprachCode)};
            return this;
        }

        public IDataBuilder AddAuftraege(IEnumerable<int> orderItemIds)
        {
            var neueAuftraege = GetAuftraege(orderItemIds);

            if (!((IDictionary<string, object>) expando).ContainsKey("Aufträge"))
            {
                expando.Aufträge = new List<Auftrag>();
            }

            var auftragsliste = (List<Auftrag>) expando.Aufträge;
            auftragsliste.AddRange(neueAuftraege);


            return this;
        }

        public IDataBuilder AddAuftraege(Ordering ordering, IEnumerable<OrderItem> items, string propertyName)
        {
            var auftraege = new List<Auftrag>();
            foreach (var orderItem in items)
            {
                auftraege.Add(GetAuftrag(ordering, orderItem));
            }

            AddValue(propertyName, auftraege);
            return this;
        }

        public IDataBuilder AddAuftrag(Ordering ordering, OrderItem item)
        {
            AddValue("Auftrag", GetAuftrag(ordering, item));
            return this;
        }

        public IDataBuilder AddValue(string propertyName, object value)
        {
            ((IDictionary<string, object>) (ExpandoObject) expando)[propertyName] = value;
            return this;
        }

        public IDataBuilder AddBestellerMitAuftraegen(int[] orderItemIds)
        {
            var auftraege = GetAuftraege(orderItemIds);
            var gruppenNachBestellerId = auftraege.GroupBy(auftrag => auftrag.Bestellung.Besteller.Id, auftrag => auftrag);

            var alleBestellerMitAuftraegen = new ListWithFlags<BestellerMitAuftraegen>();

            foreach (var gruppe in gruppenNachBestellerId)
            {
                var besteller = auftraege.First(a => a.Bestellung.Besteller.Id == gruppe.Key).Bestellung.Besteller;
                alleBestellerMitAuftraegen.Add(new BestellerMitAuftraegen(besteller, gruppe.GetEnumerator()));
            }

            expando.BestellerMitAufträgen = alleBestellerMitAuftraegen;

            return this;
        }


        public List<Auftrag> GetAuftraege(IEnumerable<int> orderItemIds)
        {
            var requestClient =
                CreateRequestClient<FindOrderItemsRequest, FindOrderItemsResponse>(bus, BusConstants.OrderManagerFindOrderItemsRequestQueue);
            var task = requestClient.Request(new FindOrderItemsRequest {OrderItemIds = orderItemIds.ToArray()});
            task.Wait();
            var response = task.Result;
            var auftraege = new List<Auftrag>();

            foreach (var orderItem in response.OrderItems)
            {
                var ordering = GetOrdering(orderItem.OrderId);

                auftraege.Add(orderItem.VeId.HasValue
                    ? GetAuftragForOrderItemWithVeId(ordering, orderItem)
                    : GetAuftragFormularbestellung(ordering, orderItem));
            }

            return auftraege;
        }

        public void Reset()
        {
            expando = new ExpandoObject();
        }

        public dynamic Create()
        {
            return expando;
        }

        private Person GetPerson(string userId)
        {
            var requestClient =
                CreateRequestClient<ReadUserInformationRequest, ReadUserInformationResponse>(bus, BusConstants.ReadUserInformationQueue);
            var task = requestClient.Request(new ReadUserInformationRequest {UserId = userId});
            task.Wait();
            var response = task.Result;
            return Person.FromUser(response?.User);
        }


        private InElasticIndexierteVe GetVe(string archiveRecordId)
        {
            return InElasticIndexierteVe.FromElasticArchiveRecord(GetElasticArchiveRecord(archiveRecordId));
        }

        private ElasticArchiveRecord GetElasticArchiveRecord(string archiveRecordId)
        {
            var requestClient =
                CreateRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse>(bus, BusConstants.IndexManagerFindArchiveRecordMessageQueue);
            var task = requestClient.Request(new FindArchiveRecordRequest {ArchiveRecordId = archiveRecordId});
            task.Wait();
            return task.Result.ElasticArchiveRecord;
        }

        private Auftrag GetAuftrag(Ordering ordering, OrderItem orderItem)
        {
            return orderItem.VeId.HasValue
                ? GetAuftragForOrderItemWithVeId(ordering, orderItem)
                : GetAuftragFormularbestellung(ordering, orderItem);
        }

        private Auftrag GetAuftragFormularbestellung(Ordering ordering, OrderItem orderItem)
        {
            var x = new BestellformularVe(orderItem);
            var besteller = GetPerson(ordering.UserId);
            var auftrag = new Auftrag(orderItem,
                ordering,
                x,
                x,
                besteller);
            return auftrag;
        }

        private Auftrag GetAuftragForOrderItemWithVeId(Ordering ordering, OrderItem orderItem)
        {
            var bestellterRecord = GetElasticArchiveRecord(orderItem.VeId.ToString());
            ElasticArchiveRecord auszuhebenderRecord = null;
            var besteller = GetPerson(ordering.UserId);

            if (ordering.Type == OrderType.Digitalisierungsauftrag)
            {
                var dossierId = bestellterRecord.GetAuszuhebendeArchiveRecordId();
                if (dossierId != null)
                {
                    auszuhebenderRecord = GetElasticArchiveRecord(dossierId);
                }
            }
            else
            {
                auszuhebenderRecord = bestellterRecord;
            }

            var auftrag = new Auftrag(orderItem,
                ordering,
                InElasticIndexierteVe.FromElasticArchiveRecord(bestellterRecord),
                InElasticIndexierteVe.FromElasticArchiveRecord(auszuhebenderRecord),
                besteller);
            return auftrag;
        }


        public static IRequestClient<T1, T2> CreateRequestClient<T1, T2>(IBus busControl, string relativeUri) where T1 : class where T2 : class
        {
            var client =
                busControl.CreateRequestClient<T1, T2>(new Uri(busControl.Address, relativeUri), TimeSpan.FromSeconds(10));

            return client;
        }

        private Ordering GetOrdering(int orderingId)
        {
            var client = CreateRequestClient<GetOrderingRequest, GetOrderingResponse>(bus, BusConstants.OrderManagerGetOrderingRequestQueue);
            var result = client.Request(new GetOrderingRequest {OrderingId = orderingId}).GetAwaiter().GetResult();
            return result.Ordering;
        }
    }
}