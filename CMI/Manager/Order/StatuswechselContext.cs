using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using CMI.Access.Common;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using CMI.Manager.Order.Status;
using CMI.Utilities.Logging.Configurator;
using MassTransit;

namespace CMI.Manager.Order
{
    /// <summary>
    ///     Der StatuswechselContext kann vom AuftragStatus verwendet werden, um auf die aktuellen
    ///     Daten des Auftrags zuzugreifen. Dazu verwendet man StatuswechselContext.Current.
    /// 
    /// </summary>
    public class StatuswechselContext
    {
        [ThreadStatic] public static StatuswechselContext current;

        private readonly List<StatusHistory> pendingStatusWechselHistoryRows;

        public StatuswechselContext(OrderItem orderItem, Ordering ordering, EMailPortfolio eMailPortfolio, User currentUser, User besteller,
            List<StatusHistory> pendingStatusWechselHistoryRows, DateTime transactionTimeStamp, ISearchIndexDataAccess searchIndexAccess, IBus bus,
            IOrderDataAccess orderDataAccess, PostCommitActionsRegistry postCommitActionsRegistry)
        {
            MailPortfolio = eMailPortfolio;
            PostCommitActionsRegistry = postCommitActionsRegistry;
            Besteller = besteller;
            TransactionTimeStamp = transactionTimeStamp;
            Bus = bus;
            OrderDataAccess = orderDataAccess;
            this.pendingStatusWechselHistoryRows = pendingStatusWechselHistoryRows;
            OrderItem = orderItem;
            Ordering = ordering;
            CurrentUser = currentUser;
            IndexAccess = searchIndexAccess;
        }

        public static StatuswechselContext Current
        {
            get => current;
            set => current = value;
        }

        public OrderItem OrderItem { get; }
        public Ordering Ordering { get; }

        /// <summary>
        ///     The current user is the user executing the command.
        ///     Most of the time this user is the same as the "Besteller".
        ///     But there is the possibility, that a BAR user is executing an order on behalf of someone.
        /// </summary>
        public User CurrentUser { get; }

        public IOrderDataAccess OrderDataAccess { get; }

        public User Besteller { get; }
        public EMailPortfolio MailPortfolio { get; }

        public PostCommitActionsRegistry PostCommitActionsRegistry { get; }
        public ISearchIndexDataAccess IndexAccess { get; }
        public IBus Bus { get; }

        public DateTime TransactionTimeStamp { get; }

        /// <summary>
        ///     Ändert den Status des aktuellen OrderItems, schreibt die History und löst OnEnter und OnLeave
        ///     auf den betroffenen Status aus.
        /// </summary>
        /// <param name="newState"></param>
        /// <param name="changedByUser"></param>
        public void SetNewStatus(AuftragStatus newState, User changedByUser = null)
        {
            var oldStatus = OrderItem.Status;

            if (oldStatus == newState.OrderStateInternal)
            {
                return; // keine Änderung, es braucht nichts historisiert zu werden
            }

            OrderItem.Status = newState.OrderStateInternal;

            pendingStatusWechselHistoryRows.Add(new StatusHistory
            {
                ChangedBy = $"{(changedByUser ?? CurrentUser).FamilyName} {(changedByUser ?? CurrentUser).FirstName}".Trim(),
                FromStatus = oldStatus,
                ToStatus = newState.OrderStateInternal,
                OrderItemId = OrderItem.Id,
                StatusChangeDate = TransactionTimeStamp
            });

            AuftragStatusRepo.GetStatus(oldStatus).OnStateLeave();
            newState.OnStateEnter();
        }

        public void SetApproveStatus(ApproveStatus newApproveStatus, User userFrom)
        {
            var oldStatus = OrderItem.ApproveStatus;

            if (oldStatus == newApproveStatus)
            {
                return; // keine Änderung, es braucht nichts historisiert zu werden
            }

            OrderItem.ApproveStatus = newApproveStatus;
            OrderItem.DatumDerFreigabe = TransactionTimeStamp;
            OrderItem.SachbearbeiterId = userFrom.Id;
        }

        public void ThrowIfUserIsNot(User user, [CallerMemberName] string memberName = "")
        {
            if (CurrentUser != user)
            {
                throw new BadRequestException("Error Executing " + memberName + ": this is only valid for User " + user);
            }
        }

        public void ThrowIfAuftragstypIsNot(IEnumerable<OrderType> allowedTypes, [CallerMemberName] string memberName = "")
        {
            var orderTypes = allowedTypes as OrderType[] ?? allowedTypes.ToArray();
            if (!orderTypes.Contains(Ordering.Type))
            {
                throw new BadRequestException(
                    $"Der Auftrag {OrderItem.Id} hat den Auftragstyp {Ordering.Type}, erlaubt sind aber nur folgende Auftragstyp(en): {string.Join(", ", orderTypes)}. Die Funktion {memberName} kann daher nicht ausgeführt werden.");
            }
        }
    }
}