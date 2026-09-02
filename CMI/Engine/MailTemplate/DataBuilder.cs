using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

        public IDataBuilder AddVesWithSameContainer(string[] containerCodes)
        {
            var behältnisList = new List<BehältnisWithEleasticRecords>();
            foreach (var containerCode in containerCodes)
            {
                var records = GetElasticArchiveRecords(containerCode, useUnanonymizedData);
                if (records.Count > 0 && records[0]?.Containers != null)
                {
                    behältnisList.Add(new BehältnisWithEleasticRecords(records, records[0].Containers.First(c => c.ContainerCode.Equals(containerCode))));
                }
            }
            expando.BehältnisList = behältnisList;
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

                auftraege.Add(!string.IsNullOrWhiteSpace(orderItem.VeId)
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


        private InElasticIndexierteVe GetVe(string archiveRecordId, UseUnanonymizedData getUnprotectedVersion)
        {
            var defaultRecord = GetElasticArchiveRecord(archiveRecordId, getUnprotectedVersion);
            var unprotectedRecord = GetElasticArchiveRecord(archiveRecordId, UseUnanonymizedData.Yes);
            return InElasticIndexierteVe.FromElasticArchiveRecord(defaultRecord, unprotectedRecord);
        }

        /// <summary>
        /// Holt den ArchiveRecord vom Elastic Index.
        /// Da wir ab und zu Timeout Probleme hatten, und dies zu unschönen resultaten führten, haben wir
        /// die Methode versucht robuster zu machen, indem bei einem Fehler (in der Regel ein RabbitMq Timeout)
        /// der Aufruf erneut versucht wird. Ebenso haben wir das Timeout des Aufrufs erhöht.
        /// </summary>
        /// <param name="archiveRecordId"></param>
        /// <param name="getUnprotectedVersion"></param>
        /// <returns></returns>
        private ElasticArchiveRecord GetElasticArchiveRecord(string archiveRecordId, UseUnanonymizedData getUnprotectedVersion)
        {
            ElasticArchiveRecord retVal;
            var retryCount = 0;
            var success = false;

            do
            {
                try
                {
                    // Bei Fehlerfall warten wir ab retryCount > 0
                    // RetryCount = 0   -->    0 ms
                    // RetryCount = 1   --> 2000 ms
                    // RetryCount = 2   --> 8000 ms
                    Thread.Sleep(1000 * ((int) Math.Pow(3, retryCount) - 1));

                    var requestClient =
                        CreateRequestClient<FindArchiveRecordRequest>(bus, BusConstants.IndexManagerFindArchiveRecordMessageQueue, 60);

                    var result = AsyncHelper.RunSync(() => requestClient.GetResponse<FindArchiveRecordResponse>(new FindArchiveRecordRequest
                    {
                        ArchiveRecordId = archiveRecordId,
                        UseUnanonymizedData = getUnprotectedVersion
                    }));

                    retVal = result.Message.ElasticArchiveRecord ?? new ElasticArchiveRecord
                    {
                        ArchiveRecordId = archiveRecordId,
                        Title = "Record not found in Elastic",
                        CreationPeriod = new ElasticTimePeriod()
                    };

                    // Could retrieve value from Elastic
                    success = true;
                }
                catch (Exception e)
                {
                    Log.Error(e,
                        "Es gab ein Problem beim Zusammenbauen von einem Record mit der id {archiveRecordId},es wird ein default Record zurückgegeben. Fehler: {message}",
                        archiveRecordId, e.Message);
                    retVal = new ElasticArchiveRecord
                    {
                        ArchiveRecordId = archiveRecordId,
                        Title = "Error while fetching record from Elastic",
                        CreationPeriod = new ElasticTimePeriod()
                    };

                    retryCount++;
                }
            } while (retryCount < 3 && !success);

            return retVal;
        }

        private List<ElasticArchiveRecord> GetElasticArchiveRecords(string containerCode, DataBuilderProtectionStatus useUnanonymizedData)
        {
            List<ElasticArchiveRecord> retVal;
            try
            {
                var requestClient =
                    CreateRequestClient<FindAllArchiveRecordsFromContainerRequest>(bus, BusConstants.IndexManagerFindArchiveRecordsWithContainerCodeMessageQueue, 600);

                var result = AsyncHelper.RunSync(() => requestClient.GetResponse<FindAllArchiveRecordsFromContainerResponse>(new FindAllArchiveRecordsFromContainerRequest
                {
                    ContainerCode = containerCode,
                    UseUnanonymizedData = useUnanonymizedData == DataBuilderProtectionStatus.AllUnanonymized ? UseUnanonymizedData.Yes : 
                        useUnanonymizedData == DataBuilderProtectionStatus.AllWithoutTitleAnonymized ? UseUnanonymizedData.NoButTitle : UseUnanonymizedData.No
                }));

                retVal = result.Message.ElasticArchiveRecords ?? new List<ElasticArchiveRecord>();
            }
            catch (Exception e)
            {
                Log.Error(e,
                    "Es gab ein Problem beim Zusammenbauen von einem Record mit der Container {containerCode},es wird ein leere Liste zurückgegeben. Fehler: {message}",
                    containerCode, e.Message);
                retVal = new List<ElasticArchiveRecord>();
            }

            return retVal;
        }


        private Auftrag GetAuftrag(Ordering ordering, OrderItem orderItem)
        {
            return string.IsNullOrWhiteSpace(orderItem.VeId)
                ? GetAuftragFormularbestellung(ordering, orderItem)
                : GetAuftragForOrderItemWithVeId(ordering, orderItem);
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
            var defaultRecord = GetElasticArchiveRecord(orderItem.VeId, AllowUnanonymized(orderItem.ApproveStatus));
            var unprotectedRecord = GetElasticArchiveRecord(orderItem.VeId, UseUnanonymizedData.Yes);

            var bestellterRecord = useUnanonymizedData == DataBuilderProtectionStatus.AllUnanonymized ? unprotectedRecord : defaultRecord;
            var unprotectedBestellterRecord = unprotectedRecord;

            ElasticArchiveRecord auszuhebenderRecord = null;
            ElasticArchiveRecord unprotectedAuszuhebenderRecord = null;
            var besteller = GetPerson(ordering.UserId);

            if (ordering.Type == OrderType.Digitalisierungsauftrag)
            {
                var dossierId = bestellterRecord.GetAuszuhebendeArchiveRecordId();
                if (dossierId != null)
                {
                    auszuhebenderRecord = GetElasticArchiveRecord(dossierId, UseUnanonymizedData.Yes);
                    unprotectedAuszuhebenderRecord = auszuhebenderRecord;
                }
            }
            else
            {
                auszuhebenderRecord = bestellterRecord;
                unprotectedAuszuhebenderRecord = unprotectedBestellterRecord;
            }

            var auftrag = new Auftrag(orderItem,
                ordering,
                InElasticIndexierteVe.FromElasticArchiveRecord(bestellterRecord, unprotectedBestellterRecord),
                InElasticIndexierteVe.FromElasticArchiveRecord(auszuhebenderRecord, unprotectedAuszuhebenderRecord),
                besteller);
            return auftrag;
        }


        public static IRequestClient<T1> CreateRequestClient<T1>(IBus busControl, string relativeUri, int timeoutInSeconds = 20) where T1 : class
        {
            var client = busControl.CreateRequestClient<T1>(new Uri(busControl.Address, relativeUri), TimeSpan.FromSeconds(timeoutInSeconds));
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
                if (!string.IsNullOrWhiteSpace(orderItem.VeId))
                {
                    if (useUnanonymizedData != DataBuilderProtectionStatus.AllAnonymized)
                    {
                        var ve = GetVe(orderItem.VeId, AllowUnanonymized(orderItem.ApproveStatus));
                        orderItem.Dossiertitel = ve.Titel;
                        orderItem.Darin = ve.Darin;
                        orderItem.ZusaetzlicheInformationen = ve.ZusaetzlicheInformationen;
                        // It is possible, that a VE record was with old ScopeId ordered
                        orderItem.VeId = ve.Id;
                    }
                }
            }
        }

        /// <summary>
        /// Depending on the approve status the user is allowed to see unanonymized data
        /// </summary>
        private UseUnanonymizedData AllowUnanonymized(ApproveStatus approveStatus)
        {
            switch (approveStatus)
            {
                case ApproveStatus.ZurueckgewiesenTeilbewilligungVorhanden:
                    return UseUnanonymizedData.NoButTitle;

                case ApproveStatus.ZurueckgewiesenEinsichtsbewilligungNoetig:
                case ApproveStatus.ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenInSchutzfrist:
                case ApproveStatus.ZurueckgewiesenFormularbestellungNichtErlaubt:
                case ApproveStatus.ZurueckgewiesenDossierangabenUnzureichend:
                case ApproveStatus.NichtGeprueft:
                    return UseUnanonymizedData.No;

                case ApproveStatus.FreigegebenDurchSystem:
                case ApproveStatus.FreigegebenAusserhalbSchutzfrist:
                case ApproveStatus.FreigegebenInSchutzfrist:
                case ApproveStatus.ZurueckgewiesenNichtFuerVerwaltungsausleiheBerechtigtUnterlagenFreiBewilligung:
                    return UseUnanonymizedData.Yes;
                default:
                    throw new ArgumentOutOfRangeException(nameof(approveStatus), approveStatus, null);
            }
        }

        private UseUnanonymizedData TryGetProtectionStatus(string archiveRecordId)
        {
            var allowUnanonymized = UseUnanonymizedData.No;
            switch (useUnanonymizedData)
            {
                case DataBuilderProtectionStatus.AllUnanonymized:
                    allowUnanonymized = UseUnanonymizedData.Yes;
                    break;
                case DataBuilderProtectionStatus.AllAnonymized:
                    allowUnanonymized = UseUnanonymizedData.No;
                    break;
                case DataBuilderProtectionStatus.AllWithoutTitleAnonymized:
                    allowUnanonymized = UseUnanonymizedData.NoButTitle;
                    break;
                case DataBuilderProtectionStatus.DependentOnApproveStatus:
                {
                    // Try to find orderitem
                    if (((IDictionary<String, object>) expando).ContainsKey("Bestellung"))
                    {
                        if (expando.Bestellung is Bestellung bestellung)
                        {
                            var orderItem = bestellung.Ordering.Items.FirstOrDefault(p => p.VeId == archiveRecordId);
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

        /// <summary>
        /// <see cref="https://github.com/aspnet/AspNetIdentity/blob/main/src/Microsoft.AspNet.Identity.Core/AsyncHelper.cs"/>
        /// </summary>
        internal static class AsyncHelper
        {
            private static readonly TaskFactory myTaskFactory = new
                TaskFactory(CancellationToken.None,
                    TaskCreationOptions.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default);

            public static TResult RunSync<TResult>(Func<Task<TResult>> func)
            {
                return AsyncHelper.myTaskFactory
                    .StartNew<Task<TResult>>(func)
                    .Unwrap<TResult>()
                    .GetAwaiter()
                    .GetResult();
            }

            public static void RunSync(Func<Task> func)
            {
                AsyncHelper.myTaskFactory
                    .StartNew<Task>(func)
                    .Unwrap()
                    .GetAwaiter()
                    .GetResult();
            }
        }
    }
}