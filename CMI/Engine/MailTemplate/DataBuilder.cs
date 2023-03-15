using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using MassTransit;
using Serilog;

namespace CMI.Engine.MailTemplate
{
    public class DataBuilder : IDataBuilder
    {
        private readonly IBus bus;
        private DataBuilderProtectionStatus useUnanonymizedData;
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
            SetProtectionStatusOfOrderItems(ordering.Items);
            expando.Bestellung = new Bestellung(ordering);
            return this;
        }

        public IDataBuilder AddVe(string archiveRecordId)
        {
            var allowUnanonymized = TryGetProtectionStatus(archiveRecordId);
            expando.Ve = GetVe(archiveRecordId, allowUnanonymized);
            return this;
        }

        public IDataBuilder AddVeList(IEnumerable<string> archiveRecordIdList)
        {
            var veList = new List<InElasticIndexierteVe>();
            foreach (var archiveRecordId in archiveRecordIdList)
            {
                var allowUnanonymized = TryGetProtectionStatus(archiveRecordId);
                veList.Add(GetVe(archiveRecordId, allowUnanonymized));
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
            var orderItems = items.ToList();
            SetProtectionStatusOfOrderItems(orderItems);
            foreach (var orderItem in orderItems)
            {
                auftraege.Add(GetAuftrag(ordering, orderItem));
            }

            AddValue(propertyName, auftraege);
            return this;
        }

        public IDataBuilder AddAuftrag(Ordering ordering, OrderItem item)
        {
            SetProtectionStatusOfOrderItems(new []{item});
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
                CreateRequestClient<FindOrderItemsRequest>(bus, BusConstants.OrderManagerFindOrderItemsRequestQueue);
            var task = requestClient.GetResponse<FindOrderItemsResponse>(new FindOrderItemsRequest {OrderItemIds = orderItemIds.ToArray()});
            task.Wait();
            var response = task.Result.Message;
            var auftraege = new List<Auftrag>();

            SetProtectionStatusOfOrderItems(response.OrderItems);
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
            useUnanonymizedData = DataBuilderProtectionStatus.AllAnonymized;
            expando = new ExpandoObject();
        }

        public dynamic Create()
        {
            return expando;
        }

        public IDataBuilder SetDataProtectionLevel(DataBuilderProtectionStatus protectionStatus)
        {
            useUnanonymizedData = protectionStatus;
            return this;
        }

        private Person GetPerson(string userId)
        {
            var requestClient =
                CreateRequestClient<ReadUserInformationRequest>(bus, BusConstants.ReadUserInformationQueue);
            var task = requestClient.GetResponse<ReadUserInformationResponse>(new ReadUserInformationRequest {UserId = userId});
            task.Wait();
            var response = task.Result.Message;
            return Person.FromUser(response?.User);
        }


        private InElasticIndexierteVe GetVe(string archiveRecordId, bool getUnprotectedVersion)
        {
            return InElasticIndexierteVe.FromElasticArchiveRecord(GetElasticArchiveRecord(archiveRecordId, getUnprotectedVersion));
        }

        private ElasticArchiveRecord GetElasticArchiveRecord(string archiveRecordId, bool getUnprotectedVersion)
        {
            try
            {
                var requestClient = CreateRequestClient<FindArchiveRecordRequest>(bus, BusConstants.IndexManagerFindArchiveRecordMessageQueue);
                var task = requestClient.GetResponse<FindArchiveRecordResponse>(new FindArchiveRecordRequest
                {
                    ArchiveRecordId = archiveRecordId,
                    UseUnanonymizedData = getUnprotectedVersion
                });
                task.Wait();
                return task.Result.Message.ElasticArchiveRecord ?? new ElasticArchiveRecord
                {
                    ArchiveRecordId = archiveRecordId,
                    Title = "Record not found in Elastic",
                    CreationPeriod = new ElasticTimePeriod()
                };
            }
            catch (Exception e)
            {
                Log.Warning("Es gab ein Problem beim Zusammenbauen von einem Record mit der id {archiveRecordId},es wird ein default Record zurückgegeben. Fehler: {message}", archiveRecordId, e.Message);
                return new ElasticArchiveRecord
                {
                    ArchiveRecordId = archiveRecordId,
                    Title = "Record not found in Elastic",
                    CreationPeriod = new ElasticTimePeriod()
                };
            }
           
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
            var bestellterRecord = useUnanonymizedData == DataBuilderProtectionStatus.AllUnanonymized ? 
                GetElasticArchiveRecord(orderItem.VeId.ToString(), true) : 
                GetElasticArchiveRecord(orderItem.VeId.ToString(), AllowUnanonymized(orderItem.ApproveStatus));
            ElasticArchiveRecord auszuhebenderRecord = null;
            var besteller = GetPerson(ordering.UserId);

            if (ordering.Type == OrderType.Digitalisierungsauftrag)
            {
                var dossierId = bestellterRecord.GetAuszuhebendeArchiveRecordId();
                if (dossierId != null)
                {
                    auszuhebenderRecord = GetElasticArchiveRecord(dossierId, true);
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


        public static IRequestClient<T1> CreateRequestClient<T1>(IBus busControl, string relativeUri) where T1 : class
        {
            var client = busControl.CreateRequestClient<T1>(new Uri(busControl.Address, relativeUri), TimeSpan.FromSeconds(10));
            return client;
        }

        private Ordering GetOrdering(int orderingId)
        {
            var client = CreateRequestClient<GetOrderingRequest>(bus, BusConstants.OrderManagerGetOrderingRequestQueue);
            var result = client.GetResponse<GetOrderingResponse>(new GetOrderingRequest {OrderingId = orderingId}).GetAwaiter().GetResult().Message;
            return result.Ordering;
        }

        private void SetProtectionStatusOfOrderItems(IEnumerable<OrderItem> orderItems)
        {
            foreach (var orderItem in orderItems)
            {
                if (orderItem.VeId.HasValue)
                {
                    if (useUnanonymizedData == DataBuilderProtectionStatus.AllUnanonymized ||
                        useUnanonymizedData == DataBuilderProtectionStatus.DependentOnApproveStatus && AllowUnanonymized(orderItem.ApproveStatus))
                    {
                        var ve = GetVe(orderItem.VeId.Value.ToString(), true);
                        orderItem.Dossiertitel = ve.Titel;
                        orderItem.Darin = ve.Darin;
                        orderItem.ZusaetzlicheInformationen = ve.ZusaetzlicheInformationen;
                    }
                }
            }
        }

        /// <summary>
        /// Depending on the approve status the user is allowed to see unanonymized data
        /// </summary>
        private bool AllowUnanonymized(ApproveStatus approveStatus)
        {
            switch (approveStatus)
            {
                case ApproveStatus.NichtGeprueft:
                case ApproveStatus.ZurueckgewiesenEinsichtsbewilligungNoetig:
                case ApproveStatus.ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenInSchutzfrist:
                case ApproveStatus.ZurueckgewiesenTeilbewilligungVorhanden:
                case ApproveStatus.ZurueckgewiesenFormularbestellungNichtErlaubt:
                case ApproveStatus.ZurueckgewiesenDossierangabenUnzureichend:
                    return false;

                case ApproveStatus.FreigegebenDurchSystem:
                case ApproveStatus.FreigegebenAusserhalbSchutzfrist:
                case ApproveStatus.FreigegebenInSchutzfrist:
                case ApproveStatus.ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenFreiBewilligung:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(approveStatus), approveStatus, null);
            }
        }

        private bool TryGetProtectionStatus(string archiveRecordId)
        {
            var allowUnanonymized = false;
            switch (useUnanonymizedData)
            {
                case DataBuilderProtectionStatus.AllUnanonymized:
                    allowUnanonymized = true;
                    break;
                case DataBuilderProtectionStatus.AllAnonymized:
                    allowUnanonymized = false;
                    break;
                case DataBuilderProtectionStatus.DependentOnApproveStatus:
                {
                    // Try to find orderitem
                    if (((IDictionary<String, object>) expando).ContainsKey("Bestellung"))
                    {
                        if (expando.Bestellung is Bestellung bestellung)
                        {
                            var orderItem = bestellung.Ordering.Items.FirstOrDefault(p => p.VeId == Convert.ToInt32(archiveRecordId));
                            if (orderItem != null)
                            {
                                allowUnanonymized = AllowUnanonymized(orderItem.ApproveStatus);
                            }
                        }
                    }

                    break;
                }
            }

            return allowUnanonymized;
        }
    }
}