using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Linq.Mapping;
using CMI.Contract.Common;
using CMI.Contract.Order;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Access.Sql.Viaduc
{
    /* KEEP IN SYNC WITH View: v_OrderingFlatItem  */
    [Table(Name = "v_OrderingFlatItem")]
    public class OrderingFlatItem
    {
        [Key]
        [Column(IsPrimaryKey = true, IsDbGenerated = true)]
        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.Id))]
        public int ItemId { get; set; }

        [Column] public int OrderingType { get; set; }

        [Column(CanBeNull = true)] public string OrderingComment { get; set; }

        [Column(CanBeNull = true)] public string OrderingArtDerArbeit { get; set; }

        [EditEinsichtsgesuchRequiresFeature(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheBereichAuftragsdatenUndBereichFreigabeBearbeiten)]
        [EditAuftragRequiresFeature(ApplicationFeature.AuftragsuebersichtAuftraegeArtDerArbeitEdit)]
        [Origin(Table = nameof(Ordering), Column = nameof(Ordering.ArtDerArbeit))]
        [Column(CanBeNull = true)]
        public int? OrderingArtDerArbeitId { get; set; }

        [EditAuftragRequiresFeature(ApplicationFeature.AuftragsuebersichtAuftraegeBereichLogistikBearbeiten)]
        [Origin(Table = nameof(Ordering), Column = nameof(Ordering.LesesaalDate))]
        [Column(CanBeNull = true)]
        public DateTime? OrderingLesesaalDate { get; set; }

        [Column] public DateTime OrderingDate { get; set; }

        [Column] public string User { get; set; }

        [Column(CanBeNull = true)] public int? VeId { get; set; }

        [EditEinsichtsgesuchRequiresFeature(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheBereichAuftragsdatenUndBereichFreigabeBearbeiten)]
        [EditAuftragRequiresFeature(ApplicationFeature.AuftragsuebersichtAuftraegeBemerkungZumDossierEdit)]
        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.Comment))]
        [Column(CanBeNull = true)]
        public string ItemComment { get; set; }

        [Column]
        [Origin(Table = nameof(Ordering), Column = nameof(Ordering.Id))]
        public int OrderId { get; set; }

        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.BewilligungsDatum))]
        [Column]
        public DateTime? BewilligungsDatum { get; set; }

        [EditAuftragRequiresFeature(ApplicationFeature.AuftragsuebersichtAuftraegePersonendatenVorhandenEdit)]
        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.HasPersonendaten))]
        [Column]
        public bool HasPersonendaten { get; set; }

        [Column] public int ApproveStatus { get; set; }

        [Column(CanBeNull = false)] public int Status { get; set; }

        [Column(CanBeNull = false)] public int ExternalStatus { get; set; }

        [Column(CanBeNull = true)]
        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.TerminDigitalisierung))]
        public DateTime? TerminDigitalisierung { get; set; }

        [EditEinsichtsgesuchRequiresFeature(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheBereichAuftragsdatenUndBereichFreigabeBearbeiten)]
        [EditAuftragRequiresFeature(ApplicationFeature.AuftragsuebersichtAuftraegeInterneBemerkungEdit)]
        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.InternalComment))]
        [Column(CanBeNull = true)]
        public string InternalComment { get; set; }

        [Column]
        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.DigitalisierungsKategorie))]
        public int DigitalisierungsKategorie { get; set; }

        [EditAuftragRequiresFeature(ApplicationFeature.AuftragsuebersichtAuftraegeBegruendungVerwaltungsausleiheEdit)]
        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.Reason))]
        [Column(CanBeNull = true)]
        public int? ReasonId { get; set; }

        [Origin(Table = nameof(Ordering), Column = nameof(Ordering.UserId))]
        [Column]
        public string UserId { get; set; }

        [Column(CanBeNull = true)] public string Reason { get; set; }

        [Column(CanBeNull = true)] public string Bestand { get; set; }

        [Column(CanBeNull = true)] public string Ablieferung { get; set; }

        [Column(CanBeNull = true)] public string BehaeltnisNummer { get; set; }

        [Column(CanBeNull = true)] public string Dossiertitel { get; set; }

        [Column(CanBeNull = true)] public string ZeitraumDossier { get; set; }

        [Column(CanBeNull = true)] public string Standort { get; set; }

        [Column(CanBeNull = true)] public string Signatur { get; set; }

        [Column(CanBeNull = true)] public string Darin { get; set; }

        [Column(CanBeNull = true)] public string ZusaetzlicheInformationen { get; set; }

        [Column(CanBeNull = true)] public string Hierarchiestufe { get; set; }

        [Column(CanBeNull = true)] public string Schutzfristverzeichnung { get; set; }

        [Column(CanBeNull = true)] public string ZugaenglichkeitGemaessBga { get; set; }

        [Column(CanBeNull = true)] public string Publikationsrechte { get; set; }

        [Column(CanBeNull = true)] public string Behaeltnistyp { get; set; }

        [Column(CanBeNull = true)] public string ZustaendigeStelle { get; set; }

        [Column(CanBeNull = true)] public string IdentifikationDigitalesMagazin { get; set; }

        [Column(CanBeNull = true)] public string ArchivNummer { get; set; }

        [Column(CanBeNull = true)] public string Aktenzeichen { get; set; }

        [Column(CanBeNull = false)] public int Eingangsart { get; set; }

        [EditAuftragRequiresFeature(ApplicationFeature.AuftragsuebersichtAuftraegeBereichLogistikBearbeiten)]
        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.Ausleihdauer))]
        [Column(CanBeNull = false)]
        public int Ausleihdauer { get; set; }

        [Column(CanBeNull = true)] public DateTime? Ausgabedatum { get; set; }

        [Column(CanBeNull = true)] public DateTime? Abschlussdatum { get; set; }

        [Column(CanBeNull = true)]
        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.MahndatumInfo))]
        public string MahndatumInfo { get; set; }

        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.AnzahlMahnungen))]
        [Column(CanBeNull = true)]
        public int AnzahlMahnungen { get; set; }

        [Column(CanBeNull = false)] public int EntscheidGesuch { get; set; }

        [Column(CanBeNull = true)] public DateTime? DatumDesEntscheids { get; set; }

        [EditEinsichtsgesuchRequiresFeature(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheBereichAuftragsdatenUndBereichFreigabeBearbeiten)]
        [Origin(Table = nameof(Ordering), Column = nameof(Ordering.BegruendungEinsichtsgesuch))]
        [Column(CanBeNull = true)]
        public string BegruendungEinsichtsgesuch { get; set; }

        [Column(CanBeNull = false)]
        [EditEinsichtsgesuchRequiresFeature(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheBereichAuftragsdatenUndBereichFreigabeBearbeiten)]
        [Origin(Table = nameof(Ordering), Column = nameof(Ordering.HasEigenePersonendaten))]
        public bool UnterlagenDieNutzerSelberBetreffen { get; set; }

        [EditEinsichtsgesuchRequiresFeature(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheBereichAuftragsdatenUndBereichFreigabeBearbeiten)]
        [Origin(Table = nameof(Ordering), Column = nameof(Ordering.PersonenbezogeneNachforschung))]
        [Column(CanBeNull = false)]
        public bool PersonenbezogeneNachforschung { get; set; }

        [Column(CanBeNull = false)] public int Abbruchgrund { get; set; }

        [Column(CanBeNull = true)] public bool? Benutzungskopie { get; set; }

        [Column(CanBeNull = true)] public DateTime? ErwartetesRueckgabeDatum { get; set; }

        [Column(CanBeNull = false)]
        [Origin(Table = nameof(Ordering), Column = nameof(Ordering.RolePublicClient))]
        public string RolePublicClient { get; set; }

        [EditAuftragRequiresFeature(ApplicationFeature.AuftragsuebersichtAuftraegeGebrauchskopieStatusEdit)]
        [Origin(Table = nameof(OrderItem), Column = nameof(OrderItem.GebrauchskopieStatus))]
        [Column(CanBeNull = false)]
        public int GebrauchskopieStatus { get; set; }
    }

    public class OriginAttribute : Attribute
    {
        public string Table { get; set; }
        public string Column { get; set; }
    }


    public class OrderingFlatDetailItem
    {
        public IEnumerable<StatusHistory> StatusHistory { get; set; }


        public IEnumerable<Bestellhistorie> OrderingHistory { get; set; }
        public bool HasMoreOrderingHistory { get; set; }

        public IEnumerable<TreeRecord> ArchivplanKontext { get; set; }

        [JsonIgnore] public OrderingFlatItem Item { get; private set; }

        [JsonExtensionData] // Wird als direkte Properties serialisiert, weil die Vererbung mit Linq2SQL nicht funktioniert.
        public JObject Data => JObject.FromObject(Item);

        public void FromFlatItem(OrderingFlatItem i)
        {
            Item = i;
        }
    }
}