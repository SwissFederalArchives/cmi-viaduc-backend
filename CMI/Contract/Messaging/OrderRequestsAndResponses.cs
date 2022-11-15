using System;
using System.Collections.Generic;
using CMI.Contract.Common;
using CMI.Contract.Order;

namespace CMI.Contract.Messaging
{
    public class AddToBasketRequest
    {
        public OrderingIndexSnapshot IndexSnapshot { get; set; }
        public string UserId { get; set; }
    }

    public class AddToBasketResponse
    {
        public OrderItem OrderItem { get; set; }
    }

    public class AddToBasketCustomRequest
    {
        public string Bestand { get; set; }
        public string Ablieferung { get; set; }
        public string BehaeltnisNummer { get; set; }
        public string ArchivNummer { get; set; }
        public string Aktenzeichen { get; set; }
        public string Dossiertitel { get; set; }
        public string ZeitraumDossier { get; set; }
        public string UserId { get; set; }
    }

    public class AddToBasketCustomResponse
    {
        public OrderItem OrderItem { get; set; }
    }

    public class RemoveFromBasketRequest
    {
        public int OrderItemId { get; set; }
        public string UserId { get; set; }
    }

    public class RemoveFromBasketResponse
    {
    }

    public class UpdateCommentRequest
    {
        public int OrderItemId { get; set; }
        public string Comment { get; set; }
        public string UserId { get; set; }
    }

    public class UpdateCommentResponse
    {
    }

    public class UpdateBenutzungskopieRequest
    {
        public int OrderItemId { get; set; }
        public bool? Benutzungskopie { get; set; }
    }

    public class UpdateBenutzungskopieResponse
    {
    }

    public class UpdateBewilligungsDatumRequest
    {
        public int OrderItemId { get; set; }
        public DateTime? BewilligungsDatum { get; set; }
        public string UserId { get; set; }
    }

    public class UpdateBewilligungsDatumResponse
    {
    }

    public class UpdateReasonRequest
    {
        public int OrderItemId { get; set; }
        public int? Reason { get; set; }
        public bool HasPersonendaten { get; set; }
        public string UserId { get; set; }
    }

    public class UpdateReasonResponse
    {
    }

    public class GetBasketRequest
    {
        public string UserId { get; set; }
    }

    public class GetBasketResponse
    {
        public IEnumerable<OrderItem> OrderItems { get; set; }
    }

    public class GetOrderingsRequest
    {
        public string UserId { get; set; }
    }

    public class GetOrderingsResponse
    {
        public IEnumerable<Ordering> Orderings { get; set; }
    }

    public class GetOrderingRequest
    {
        public int OrderingId { get; set; }
    }

    public class GetOrderingResponse
    {
        public Ordering Ordering { get; set; }
    }

    public class IsUniqueVeInBasketRequest
    {
        public int VeId { get; set; }
        public string UserId { get; set; }
    }

    public class IsUniqueVeInBasketResponse
    {
        public bool IsUniqueVeInBasket { get; set; }
    }

    public class GetDigipoolRequest
    {
        public int NumberOfEntries { get; set; }
    }

    public class GetDigipoolResponse
    {
        public DigipoolEntry[] GetDigipool { get; set; }
    }

    public class UpdateDigipoolRequest
    {
        public List<int> OrderItemIds { get; set; }
        public int? DigitalisierungsKategorie { get; set; }
        public DateTime? TerminDigitalisierung { get; set; }
    }

    public class UpdateDigipoolResponse
    {
    }
    
    public class GetPrimaerdatenReportRecordsRequest
    {
        public LogDataFilter Filter { get; set; }
    }

    public class GetPrimaerdatenReportRecordsResponse
    {
        public List<PrimaerdatenAufbereitungItem> Items { get; set; }
    }

    public class GetStatusHistoryForOrderItemRequest
    {
        public int OrderItemId { get; set; }
    }

    public class GetStatusHistoryForOrderItemResponse
    {
        public IEnumerable<StatusHistory> StatusHistory { get; set; }
    }

    public class EntscheidFreigabeHinterlegenRequest
    {
        public List<int> OrderItemIds { get; set; }
        public ApproveStatus Entscheid { get; set; }
        public DateTime? DatumBewilligung { get; set; }
        public string InterneBemerkung { get; set; }
        public string UserId { get; set; }
    }

    public class EntscheidFreigabeHinterlegenResponse
    {
    }

    public class EntscheidGesuchHinterlegenRequest
    {
        public List<int> OrderItemIds { get; set; }
        public EntscheidGesuch Entscheid { get; set; }
        public DateTime DatumEntscheid { get; set; }
        public string InterneBemerkung { get; set; }
        public string UserId { get; set; }
    }

    public class EntscheidGesuchHinterlegenResponse
    {
    }

    public class AushebungsauftraegeDruckenRequest
    {
        public List<int> OrderItemIds { get; set; }
        public string UserId { get; set; }
    }

    public class AushebungsauftraegeDruckenResponse
    {
    }


    public class RecalcIndivTokens
    {
        public int ArchiveRecordId { get; set; }
        public string[] ExistingPrimaryDataDownloadAccessTokens { get; set; }
        public string[] ExistingPrimaryDataFulltextAccessTokens { get; set; }
        public string[] ExistingMetadataAccessTokens { get; set; }
        public string[] ExistingFieldAccessTokens { get; set; }
    }

    public class UpdateIndivTokens
    {
        public long ArchiveRecordId { get; set; }

        public string[] CombinedPrimaryDataDownloadAccessTokens { get; set; }
        public string[] CombinedPrimaryDataFulltextAccessTokens { get; set; }
        public string[] CombinedMetadataAccessTokens { get; set; }
        public string[] CombinedFieldAccessTokens { get; set; }
    }

    public class InVorlageExportierenRequest
    {
        public string CurrentUserId { get; set; }
        public List<int> OrderItemIds { get; set; }
        public Vorlage Vorlage { get; set; }
        public string Sprache { get; set; }
    }

    public class InVorlageExportierenResponse
    {
    }


    public class AbschliessenRequest
    {
        public string CurrentUserId { get; set; }
        public List<int> OrderItemIds { get; set; }
    }

    public class AbschliessenResponse
    {
    }

    public class AbbrechenRequest
    {
        public string CurrentUserId { get; set; }
        public List<int> OrderItemIds { get; set; }
        public Abbruchgrund Abbruchgrund { get; set; }
        public string BemerkungZumDossier { get; set; }
        public string InterneBemerkung { get; set; }
    }

    public class AbbrechenResponse
    {
    }

    public class ZuruecksetzenRequest
    {
        public string CurrentUserId { get; set; }
        public List<int> OrderItemIds { get; set; }
    }

    public class ZuruecksetzenResponse
    {
    }

    public class AuftraegeAusleihenRequest
    {
        public string CurrentUserId { get; set; }
        public List<int> OrderItemIds { get; set; }
    }

    public class AuftraegeAusleihenResponse
    {
    }

    public class DigitalisierungAusloesenRequest
    {
        public string CurrentUserId { get; set; }
        public OrderingIndexSnapshot[] Snapshots { get; set; }
        public int ArtDerArbeit { get; set; }
    }

    public class DigitalisierungAusloesenResponse
    {
    }

    public class MarkOrderAsFaultedRequest
    {
        public int OrderItemId { get; set; }
    }

    public class MarkOrderAsFaultedResponse
    {
    }

    public class ResetAufbereitungsfehlerRequest
    {
        public List<int> OrderItemIds { get; set; }
    }

    public class ResetAufbereitungsfehlerResponse
    {
    }

    public class UpdateOrderItemRequest
    {
        public OrderItem OrderItem { get; set; }
    }

    public class UpdateOrderItemResponse
    {
        public int OrderItemId { get; set; }
    }

}