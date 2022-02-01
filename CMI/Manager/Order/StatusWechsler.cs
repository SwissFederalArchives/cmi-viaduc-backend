using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Transactions;
using CMI.Access.Common;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using CMI.Manager.Order.Status;
using MassTransit;

namespace CMI.Manager.Order
{
    public class StatusWechsler
    {
        private readonly Dictionary<string, User> bestellerByUserId = new Dictionary<string, User>();
        private readonly IBus bus;
        private readonly EMailPortfolio eMailPortfolio = new EMailPortfolio();
        private readonly IOrderDataAccess orderDataAccess;
        private readonly Dictionary<int, Ordering> orderingsById = new Dictionary<int, Ordering>();
        private readonly List<StatusHistory> pendingStatusWechselHistoryRows = new List<StatusHistory>();
        private readonly PostCommitActionsRegistry postCommitActions = new PostCommitActionsRegistry();
        private readonly ISearchIndexDataAccess searchIndexDataAccess;
        private readonly IUserDataAccess userDataAccess;
        private bool hasExecuted;
        private OrderItem[] orderItems;
        private DateTime transactionDateTime;

        public StatusWechsler(IOrderDataAccess orderDataAccess, IUserDataAccess userDataAccess, ISearchIndexDataAccess searchIndexDataAccess, IBus bus)
        {
            this.orderDataAccess = orderDataAccess;
            this.userDataAccess = userDataAccess;
            this.searchIndexDataAccess = searchIndexDataAccess;
            this.bus = bus;
        }

        public User CurrentUser { get; set; }

        public async Task Execute(Action<IAuftragsAktionen> aktion, OrderItem[] orderItems, User currentUser, DateTime transactionDateTime)
        {
            if (hasExecuted)
            {
                throw new Exception(
                    "Statuswechsler.Execute() should only be called once, but the Execute Method was called before on this Statuswechsler.");
            }

            hasExecuted = true;

            this.orderItems = orderItems;

            this.transactionDateTime = transactionDateTime;
            CurrentUser = currentUser;
            await LoadOrderings();
            LoadBesteller();
            await Verarbeiten(aktion);
        }

        /// <summary>
        ///     Lädt zu jedem OrderItem das entsprechende Ordering von der DB.
        ///     Das Ordering muss auch seine OrderItems enthalten.
        ///     Dabei sicherstellen, dass ein-und dasselbe OrderItem in Ordering.OrderItems
        ///     auftaucht, wie auch in den orderItems[] enthalten ist.
        /// </summary>
        private async Task LoadOrderings()
        {
            foreach (var orderItem in orderItems)
            {
                if (!orderingsById.TryGetValue(orderItem.OrderId, out var ordering))
                {
                    ordering = await orderDataAccess.GetOrdering(orderItem.OrderId);
                    orderingsById[orderItem.OrderId] = ordering;
                }

                // OrderItems ersetzen, damit nicht doppel geladen:
                for (var idx = 0; idx < ordering.Items.Length; idx++)
                {
                    if (ordering.Items[idx].Id == orderItem.Id)
                    {
                        ordering.Items[idx] = orderItem;
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Lädt zu jedem Ordering den Besteller von der DB.
        /// </summary>
        private void LoadBesteller()
        {
            foreach (var ordering in orderingsById.Values)
            {
                if (!bestellerByUserId.ContainsKey(ordering.UserId))
                {
                    bestellerByUserId[ordering.UserId] =
                        userDataAccess.GetUser(ordering.UserId); // Wichtig: Ämter müssen geladen werden, ansonsten keine Amtstoken
                }
            }
        }

        private async Task Verarbeiten(Action<AuftragStatus> action)
        {
            PerformActionWithContextForAll(action);
            await SaveToDb();
            await ((IRunAll) postCommitActions).RunAll();
            await ((ISendable) eMailPortfolio).Send(bus);
        }

        private void PerformActionWithContextForAll(Action<AuftragStatus> action)
        {
            foreach (var orderItem in orderItems)
            {
                var ordering = orderingsById[orderItem.OrderId];
                var besteller = bestellerByUserId[ordering.UserId];

                StatuswechselContext.Current = new StatuswechselContext(orderItem, ordering, eMailPortfolio, CurrentUser, besteller,
                    pendingStatusWechselHistoryRows, transactionDateTime, searchIndexDataAccess, bus, orderDataAccess, postCommitActions);
                var status = AuftragStatusRepo.GetStatus(orderItem.Status);
                action(status);
                StatuswechselContext.current = null;
            }
        }

        private async Task SaveToDb()
        {
            using (var t = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
            {
                foreach (var orderItem in orderItems)
                {
                    await orderDataAccess.UpdateOrderItem(orderItem);
                }

                foreach (var statusHistoryRow in pendingStatusWechselHistoryRows)
                {
                    await orderDataAccess.AddStatusHistoryRecord(statusHistoryRow.OrderItemId, statusHistoryRow.FromStatus, statusHistoryRow.ToStatus,
                        statusHistoryRow.ChangedBy);
                }

                t.Complete();
            }
        }
    }
}