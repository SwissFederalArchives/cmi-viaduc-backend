using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMI.Contract.Order
{
    public interface IPublicOrder
    {
        Task<OrderItem> AddToBasket(OrderingIndexSnapshot indexSnapshot, string userId);

        Task<OrderItem> AddToBasketCustom(string bestand, string ablieferung, string behaeltnisNummer, string archivNummer, string aktenzeichen,
            string dossiertitel, string zeitraumDossier, string userId);

        Task RemoveFromBasket(int orderItemId, string userId);
        Task UpdateComment(int orderItemId, string comment, string userId);
        Task UpdateBenutzungskopie(int orderItemId, bool? benutzungskopie);
        Task UpdateBewilligungsDatum(int orderItemId, DateTime? bewilligungsDatum, string userId);
        Task UpdateReason(int orderItemId, int? reason, bool hasPersonendaten, string userId);
        Task<IEnumerable<OrderItem>> GetBasket(string userId);
        Task UpdateOrderDetail(UpdateOrderDetailData updateData);
        Task CreateOrderFromBasket(OrderCreationRequest creationRequest);
        Task<IEnumerable<Ordering>> GetOrderings(string userId);
        Task<Ordering> GetOrdering(int orderingId);

        Task<OrderItem[]> FindOrderItems(int[] orderItemIds);

        Task<bool> IsUniqueVeInBasket(int ve, string userId);

        Task<DigipoolEntry[]> GetDigipool(int numberOfEntries);
        Task UpdateDigipool(List<int> orderItemIds, int? digitalisierungsKategorie, DateTime? terminDigitalisierung);
        Task<IEnumerable<StatusHistory>> GetStatusHistoryForOrderItem(int orderItemId);
        Task<List<Bestellhistorie>> GetOrderingHistoryForVe(int veId);

        Task EntscheidFreigabeHinterlegen(string currentUserId, List<int> orderItemIds, ApproveStatus entscheid, DateTime? datumBewilligung,
            string interneBemerkung);

        Task AushebungsauftraegeDrucken(string currentUserId, List<int> orderItemIds);

        Task EntscheidGesuchHinterlegen(string currentUserId, List<int> pOrderItemIds, EntscheidGesuch entscheid, DateTime datumEntscheid,
            string interneBemerkung);

        Task InVorlageExportieren(string currentUserId, List<int> orderItemIds, Vorlage vorlage, string sprache);
        Task ZumReponierenBereit(string currentUserId, List<int> orderItemsId);
        Task Abschliessen(string currentUserId, List<int> orderItemIds);
        Task Zuruecksetzen(string currentUserId, List<int> orderItemIds);
        Task Abbrechen(string currentUserId, List<int> orderItemIds, Abbruchgrund abbruchgrund, string bemerkungZumDossier, string interneBemerkung);
        Task AuftraegeAusleihen(string currentUserId, List<int> orderItemIds);
        Task DigitalisierungAusloesen(string currentUserId, OrderingIndexSnapshot[] snapshots, int artDerArbeit);
        Task MarkOrderAsFaulted(int orderItemId);
        Task ResetAufbereitungsfehler(List<int> orderItemIds);
        Task<MahnungVersendenResponse> MahnungVersenden(List<int> orderItemIds, string language, int gewaehlteMahnungAnzahl, string userId);
    }
}