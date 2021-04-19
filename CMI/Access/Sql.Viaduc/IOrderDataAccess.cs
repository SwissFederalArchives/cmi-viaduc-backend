using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Order;

namespace CMI.Access.Sql.Viaduc
{
    public interface IOrderDataAccess : IDigipoolDataAccess
    {
        Task<OrderItem> AddToBasket(OrderingIndexSnapshot indexSnapshot, string userId);

        Task<OrderItem> AddToBasket(string bestand, string ablieferung, string behaeltnisNummer, string archivNummer,
            string aktenzeichen, string dossiertitel, string zeitraumDossier, string userId);

        Task RemoveFromBasket(int orderItemId, string userId);

        Task UpdateBewilligung(int orderItemId, DateTime? bewilligungsDatum, string userId);
        Task UpdateComment(int orderItemId, string comment, string userId);
        Task UpdateBenutzungskopie(int orderItemId, bool? benutzungskopie);
        Task UpdateBenutzungskopieStatus(int orderItemId, GebrauchskopieStatus gebrauchskopieStatus);
        Task UpdateReason(int orderItemId, int? reasonId, bool hasPersonendaten, string userId);

        Task<IEnumerable<OrderItem>> GetBasket(string userId);

        Task<int> CreateOrderFromBasket(OrderCreationRequest orderCreationRequest);

        Task<IEnumerable<Ordering>> GetOrderings(string userId);

        Task<Ordering> GetOrdering(int orderingId, bool includeOrderItems = true);

        Task<OrderItem> GetOrderItem(int orderItemId);
        Task<int> UpdateOrderItem(OrderItem orderItem);

        Task AddStatusHistoryRecord(int orderItemId, OrderStatesInternal from, OrderStatesInternal to,
            string changedBy);

        Task AddApproveHistoryRecord(int orderItemId, string approvedToUser, OrderType orderType,
            ApproveStatus from, ApproveStatus to, string changedBy);

        Task<StatusHistory[]> GetStatusHistoryForOrderItem(int orderItemId);

        Task<OrderItem[]> FindOrderItems(int[] orderItemIds);

        Task<IndivTokens> GetIndividualAccessTokens(int veId, int ignoreOrderItemId = -1);

        Task<bool> HasEinsichtsbewilligung(int veId);

        Task ChangeUserForOrdering(int orderingId, string newUserId);

        Task UpdateOrderDetail(UpdateOrderDetailData data);

        Task<bool> IsUniqueVeInBasket(int veId, string userId);

        Task<List<Bestellhistorie>> GetOrderingHistoryForVe(int veId);

        Task AddToOrderExecutedWaitList(int veId, string serializedMessage);

        Task MarkOrderAsProcessedInWaitList(int waitListId);

        Task<List<OrderExecutedWaitList>> GetVeFromOrderExecutedWaitList(int veId);

        Task<List<OrderItemByUser>> GetOrderItemsByUser(int[] orderItemIds);
    }

    public interface IDigipoolDataAccess
    {
        Task<DigipoolEntry[]> GetDigipool();

        Task<List<DigitalisierungsTermin>> GetLatestDigitalisierungsTermine(string userId, DateTime fromDate, DigitalisierungsKategorie kategorie);

        Task UpdateDigipool(List<int> orderItemIds, int? digitalisierungsKategorie, DateTime? terminDigitalisierung);
        Task UpdateTermin(int orderItemId, DateTime terminDigitalisierung);
    }
}