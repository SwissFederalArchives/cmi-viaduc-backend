using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Manager.Order.Mails;
using CMI.Manager.Order.Status;
using CMI.Utilities.Template;
using Serilog;
using IBus = MassTransit.IBus;

namespace CMI.Manager.Order
{
    public class OrderManager : IPublicOrder
    {
        private readonly IBus bus;
        private readonly IDataBuilder dataBuilder;
        private readonly IMailHelper mailHelper;
        private readonly IParameterHelper parameterHelper;
        private readonly IOrderDataAccess sqlDataAccess;
        private readonly StatusWechsler statusWechsler;
        private readonly IUserDataAccess userAccess;

        public OrderManager(StatusWechsler statusWechsler, IOrderDataAccess sqlDataAccess, IUserDataAccess userAccess,
            IParameterHelper parameterHelper, IBus bus, IMailHelper mailHelper, IDataBuilder dataBuilder)
        {
            this.statusWechsler = statusWechsler;
            this.sqlDataAccess = sqlDataAccess;
            this.userAccess = userAccess;
            this.parameterHelper = parameterHelper;
            this.bus = bus;
            this.mailHelper = mailHelper;
            this.dataBuilder = dataBuilder;
        }

        public Task<OrderItem> AddToBasket(OrderingIndexSnapshot indexSnapshot, string userId)
        {
            return sqlDataAccess.AddToBasket(indexSnapshot, userId);
        }

        public Task<OrderItem> AddToBasketCustom(string bestand, string ablieferung, string behaeltnisNummer, string archivNummer,
            string aktenzeichen, string dossiertitel, string zeitraumDossier, string userId)
        {
            return sqlDataAccess.AddToBasket(bestand, ablieferung, behaeltnisNummer, archivNummer, aktenzeichen, dossiertitel, zeitraumDossier,
                userId);
        }

        public Task RemoveFromBasket(int orderItemId, string userId)
        {
            return sqlDataAccess.RemoveFromBasket(orderItemId, userId);
        }

        public Task UpdateComment(int orderItemId, string comment, string userId)
        {
            return sqlDataAccess.UpdateComment(orderItemId, comment, userId);
        }

        public Task UpdateBenutzungskopie(int orderItemId, bool? benutzungskopie)
        {
            return sqlDataAccess.UpdateBenutzungskopie(orderItemId, benutzungskopie);
        }

        public Task UpdateBewilligungsDatum(int orderItemId, DateTime? bewilligungsDatum, string userId)
        {
            return sqlDataAccess.UpdateBewilligung(orderItemId, bewilligungsDatum, userId);
        }

        public Task UpdateReason(int orderItemId, int? reason, bool hasPersonendaten, string userId)
        {
            return sqlDataAccess.UpdateReason(orderItemId, reason, hasPersonendaten, userId);
        }

        public Task<IEnumerable<OrderItem>> GetBasket(string userId)
        {
            return sqlDataAccess.GetBasket(userId);
        }

        public Task UpdateOrderDetail(UpdateOrderDetailData updateData)
        {
            return sqlDataAccess.UpdateOrderDetail(updateData);
        }

        public async Task CreateOrderFromBasket(OrderCreationRequest creationRequest)
        {
            using (var t = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                // Transform the basket into an order
                var orderingId = await sqlDataAccess.CreateOrderFromBasket(creationRequest);

                if (creationRequest.BestellerId != creationRequest.CurrentUserId)
                {
                    await sqlDataAccess.ChangeUserForOrdering(orderingId, creationRequest.BestellerId);
                }

                // get the order items of the order
                var order = await sqlDataAccess.GetOrdering(orderingId);

                if (creationRequest.FunktionDigitalisierungAusloesen)
                {
                    // Digitalisierungs-Kategorie Gesuch setzen
                    await sqlDataAccess.UpdateDigipool(order.Items.Select(i => i.Id).ToList(), (int) DigitalisierungsKategorie.Gesuch, null);
                    foreach (var orderItem in order.Items)
                    {
                        orderItem.DigitalisierungsKategorie = DigitalisierungsKategorie.Gesuch;
                    }
                }

                // Execute state change
                await statusWechsler.Execute(oi => oi.Bestellen(), order.Items, GetUser(creationRequest.CurrentUserId), DateTime.Now);

                // If we are here, we can commit all pending updates
                t.Complete();
            }
        }

        public Task<IEnumerable<Ordering>> GetOrderings(string userId)
        {
            return sqlDataAccess.GetOrderings(userId);
        }

        public Task<Ordering> GetOrdering(int orderingId)
        {
            return sqlDataAccess.GetOrdering(orderingId);
        }

        public Task<OrderItem[]> FindOrderItems(int[] orderItemIds)
        {
            return sqlDataAccess.FindOrderItems(orderItemIds);
        }

        public Task<bool> IsUniqueVeInBasket(int veId, string userId)
        {
            return sqlDataAccess.IsUniqueVeInBasket(veId, userId);
        }

        public async Task<DigipoolEntry[]> GetDigipool(int numberOfEntries)
        {
            try
            {
                var entryArray = await sqlDataAccess.GetDigipool();

                foreach (var entry in entryArray)
                {
                    SetPriority(entry);
                }

                return entryArray.OrderBy(e => e.Priority)
                    .ThenBy(e => e.TerminDigitalisierung)
                    .ThenBy(e => e.OrderItemId)
                    .Take(numberOfEntries)
                    .ToArray();
            }
            catch (SqlException ex)
            {
                Log.Error(ex, "SQL Server problem");

                // Only really handle the case where connection could not be made
                if (ex.Errors[0].Number == 2 || ex.Errors[0].Number == 17142)
                {
                    throw new OrderDatabaseNotFoundOrNotRunningException();
                }

                throw;
            }
        }

        public Task UpdateDigipool(List<int> orderItemIds, int? digitalisierungsKategorie, DateTime? terminDigitalisierung)
        {
            return sqlDataAccess.UpdateDigipool(orderItemIds, digitalisierungsKategorie, terminDigitalisierung);
        }

        public async Task<IEnumerable<StatusHistory>> GetStatusHistoryForOrderItem(int orderItemId)
        {
            return await sqlDataAccess.GetStatusHistoryForOrderItem(orderItemId);
        }

        public Task<List<Bestellhistorie>> GetOrderingHistoryForVe(int veId)
        {
            return sqlDataAccess.GetOrderingHistoryForVe(veId);
        }

        public async Task EntscheidFreigabeHinterlegen(string currentUserId, List<int> orderItemIds, ApproveStatus entscheid,
            DateTime? datumBewilligung,
            string interneBemerkung)
        {
            var orderList = await GetOrderItems(orderItemIds);
            await statusWechsler.Execute(oi => oi.EntscheidFreigabeHinterlegen(entscheid, datumBewilligung, interneBemerkung), orderList,
                GetUser(currentUserId), DateTime.Now);
        }

        public async Task AushebungsauftraegeDrucken(string currentUserId, List<int> orderItemIds)
        {
            var orderList = await GetOrderItems(orderItemIds);
            await statusWechsler.Execute(oi => oi.AushebungsauftragDrucken(), orderList, GetUser(currentUserId), DateTime.Now);
        }

        public async Task EntscheidGesuchHinterlegen(string currentUserId, List<int> orderItemIds, EntscheidGesuch entscheid,
            DateTime datumEntscheid, string interneBemerkung)
        {
            var orderList = await GetOrderItems(orderItemIds);
            await statusWechsler.Execute(oi => oi.EntscheidGesuchHinterlegen(entscheid, datumEntscheid, interneBemerkung), orderList,
                GetUser(currentUserId), DateTime.Now);
        }

        public async Task InVorlageExportieren(string currentUserId, List<int> orderItemIds, Vorlage vorlage, string sprache)
        {
            var orderList = await GetOrderItems(orderItemIds);
            await statusWechsler.Execute(oi => oi.InVorlageExportieren(vorlage, sprache), orderList, GetUser(currentUserId), DateTime.Now);
        }

        public Task ZumReponierenBereit(string currentUserId, List<int> orderItemsId)
        {
            return Task.CompletedTask; // wird im SetStatusZumReponierenBereitConsumer gehandelt
        }

        public async Task Abschliessen(string currentUserId, List<int> orderItemIds)
        {
            var orderList = await GetOrderItems(orderItemIds);
            await statusWechsler.Execute(oi => oi.Abschliessen(), orderList, GetUser(currentUserId), DateTime.Now);
        }

        public async Task Abbrechen(string currentUserId, List<int> orderItemIds, Abbruchgrund abbruchgrund, string bemerkungZumDossier,
            string interneBemerkung)
        {
            var orderList = await GetOrderItems(orderItemIds);
            await statusWechsler.Execute(oi => oi.Abbrechen(abbruchgrund, bemerkungZumDossier, interneBemerkung),
                orderList, GetUser(currentUserId), DateTime.Now);
        }

        public async Task Zuruecksetzen(string currentUserId, List<int> orderItemIds)
        {
            var orderList = await GetOrderItems(orderItemIds);
            await statusWechsler.Execute(oi => oi.Zuruecksetzen(), orderList, GetUser(currentUserId), DateTime.Now);
        }

        public async Task AuftraegeAusleihen(string currentUserId, List<int> orderItemIds)
        {
            var orderList = await GetOrderItems(orderItemIds);
            await statusWechsler.Execute(oi => oi.Ausleihen(), orderList, GetUser(currentUserId), DateTime.Now);
        }

        public async Task DigitalisierungAusloesen(string currentUserId, OrderingIndexSnapshot[] snapshots, int artDerArbeit)
        {
            using (var t = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                var addedItems = new List<OrderItem>();
                foreach (var snapshot in snapshots)
                {
                    var orderItemDb = await AddToBasket(snapshot, currentUserId);
                    addedItems.Add(orderItemDb);
                }

                var allBasketItems = await GetBasket(currentUserId);
                var excludedItemIds = allBasketItems.Where(i => addedItems.All(addedItem => addedItem.Id != i.Id)).Select(x => x.Id).ToList();
                var creationRequest = new OrderCreationRequest
                {
                    OrderItemIdsToExclude = excludedItemIds,
                    Type = OrderType.Digitalisierungsauftrag,
                    LesesaalDate = null,
                    ArtDerArbeit = artDerArbeit,
                    BegruendungEinsichtsgesuch = null,
                    Comment = null,
                    CurrentUserId = currentUserId,
                    BestellerId = currentUserId,
                    FunktionDigitalisierungAusloesen = true
                };

                await CreateOrderFromBasket(creationRequest);

                t.Complete();
            }
        }

        public async Task MarkOrderAsFaulted(int orderItemId)
        {
            var orderItem = await sqlDataAccess.GetOrderItem(orderItemId);
            orderItem.HasAufbereitungsfehler = true;
            await sqlDataAccess.UpdateOrderItem(orderItem);
        }

        public async Task ResetAufbereitungsfehler(List<int> orderItemIds)
        {
            foreach (var orderItemId in orderItemIds)
            {
                var orderItem = await sqlDataAccess.GetOrderItem(orderItemId);
                orderItem.HasAufbereitungsfehler = false;
                await sqlDataAccess.UpdateOrderItem(orderItem);
            }
        }

        public async Task<MahnungVersendenResponse> MahnungVersenden(List<int> orderItemIds, string language, int gewaehlteMahnungAnzahl,
            string userId)
        {
            try
            {
                var orderItemsByUser = await sqlDataAccess.GetOrderItemsByUser(orderItemIds.ToArray());

                foreach (var itemByUser in orderItemsByUser)
                {
                    dataBuilder.Reset();
                    var dataContext = dataBuilder
                        .AddAuftraege(itemByUser.OrderItemIds)
                        .AddBesteller(itemByUser.UserId)
                        .AddUser(userId)
                        .AddSprache(language)
                        .AddValue("RückgabeTermin", DateTime.Today.AddDays(30).ToString("dd.MM.yyyy"))
                        .Create();
                    switch (gewaehlteMahnungAnzahl)
                    {
                        case 1:
                            // Sennde Mail Vorlage 1
                            var template1 = parameterHelper.GetSetting<AusleiheErsteMahnung>();
                            var user = userAccess.GetUser(itemByUser.UserId);
                            template1.To = user.EmailAddress;
                            await mailHelper.SendEmail(bus, template1, dataContext);
                            break;
                        case 2:
                            // Sende Mail Vorlage 2
                            var template2 = parameterHelper.GetSetting<AusleiheZweiteMahnung>();
                            var initiatingUser = userAccess.GetUser(userId);
                            template2.To = initiatingUser.EmailAddress;
                            await mailHelper.SendEmail(bus, template2, dataContext);
                            break;
                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }

                await UpdateMahnungInfoForOrderItems(orderItemIds, gewaehlteMahnungAnzahl, userId);

                return new MahnungVersendenResponse {Success = true};
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexptected error while sending Mahnungen");
                return new MahnungVersendenResponse {Success = false, Error = ex.Message};
            }
        }

        public User GetUser(string orderUserId)
        {
            return userAccess.GetUser(orderUserId);
        }


        private void SetPriority(DigipoolEntry digipoolEntry)
        {
            var digitalisierunskategorie = (DigitalisierungsKategorie) digipoolEntry.Digitalisierunskategorie;
            var terminBevorOrEqualToday = digipoolEntry.TerminDigitalisierung.Date <= DateTime.Now.Date;

            if (digitalisierunskategorie == DigitalisierungsKategorie.Spezial)
            {
                digipoolEntry.Priority = 1;
            }
            else if ((digitalisierunskategorie == DigitalisierungsKategorie.Oeffentlichkeit ||
                      digitalisierunskategorie == DigitalisierungsKategorie.Forschungsgruppe) &&
                     terminBevorOrEqualToday)
            {
                digipoolEntry.Priority = 2;
            }
            else if (digitalisierunskategorie == DigitalisierungsKategorie.Gesuch && terminBevorOrEqualToday)
            {
                digipoolEntry.Priority = 3;
            }
            else if (digitalisierunskategorie == DigitalisierungsKategorie.Amt && terminBevorOrEqualToday)
            {
                digipoolEntry.Priority = 4;
            }
            else if (digitalisierunskategorie == DigitalisierungsKategorie.Intern && terminBevorOrEqualToday)
            {
                digipoolEntry.Priority = 5;
            }
            else if (digitalisierunskategorie == DigitalisierungsKategorie.Termin && terminBevorOrEqualToday)
            {
                digipoolEntry.Priority = 6;
            }
            else if (digitalisierunskategorie == DigitalisierungsKategorie.Amt)
            {
                digipoolEntry.Priority = 7;
            }
            else
            {
                digipoolEntry.Priority = 8;
            }

            // Die zuvor ermittelte Priorität wird übersteuert, wenn ein Aufbereitungsfehler vorliegt
            if (digipoolEntry.HasAufbereitungsfehler)
            {
                digipoolEntry.Priority = 9;
            }
        }

        private async Task<OrderItem[]> GetOrderItems(List<int> ids)
        {
            var list = new List<OrderItem>();

            foreach (var id in ids)
            {
                var item = await sqlDataAccess.GetOrderItem(id);
                list.Add(item);
            }

            return list.ToArray();
        }

        private async Task UpdateMahnungInfoForOrderItems(List<int> orderItemIds, int gewaehlteMahnungAnzahl, string userId)
        {
            var user = userAccess.GetUser(userId);

            foreach (var orderItemId in orderItemIds)
            {
                var orderItem = await sqlDataAccess.GetOrderItem(orderItemId);

                orderItem.AnzahlMahnungen++;
                var anzahlText = gewaehlteMahnungAnzahl == 1 ? "Erste" : "Zweite";
                orderItem.InternalComment =
                    orderItem.InternalComment.Prepend(
                        $"{anzahlText} Mahnung versandt {DateTime.Now:dd.MM.yyyy HH:mm:ss}, {user.FirstName} {user.FamilyName}");
                orderItem.MahndatumInfo = orderItem.MahndatumInfo.Prepend($"{anzahlText} Mahnung {DateTime.Now:dd.MM.yyyy HH:mm:ss}");

                await sqlDataAccess.UpdateOrderItem(orderItem);
            }
        }
    }
}