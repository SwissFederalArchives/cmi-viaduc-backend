using System;

namespace CMI.Contract.Order
{
    public class OrderItem
    {
        public int Id { get; set; }
        public int? VeId { get; set; }
        public string Comment { get; set; }
        public int OrderId { get; set; }
        public DateTime? BewilligungsDatum { get; set; }
        public bool HasPersonendaten { get; set; }
        public OrderStatesInternal Status { get; set; }
        public string Bestand { get; set; }
        public string Ablieferung { get; set; }
        public string BehaeltnisNummer { get; set; }
        public string Dossiertitel { get; set; }
        public string ZeitraumDossier { get; set; }
        public string ArchivNummer { get; set; }
        public string Standort { get; set; }
        public string Signatur { get; set; }
        public string Darin { get; set; }
        public string ZusaetzlicheInformationen { get; set; }
        public string Hierarchiestufe { get; set; }
        public string Schutzfristverzeichnung { get; set; }
        public string ZugaenglichkeitGemaessBga { get; set; }
        public string Publikationsrechte { get; set; }
        public string Behaeltnistyp { get; set; }
        public string ZustaendigeStelle { get; set; }
        public string IdentifikationDigitalesMagazin { get; set; }
        public ApproveStatus ApproveStatus { get; set; }
        public int? Reason { get; set; }
        public string Aktenzeichen { get; set; }
        public DigitalisierungsKategorie DigitalisierungsKategorie { get; set; }
        public DateTime? TerminDigitalisierung { get; set; }
        public string InternalComment { get; set; }

        public EntscheidGesuch EntscheidGesuch { get; set; }
        public DateTime? DatumDesEntscheids { get; set; }
        public DateTime? Ausgabedatum { get; set; }
        public DateTime? Abschlussdatum { get; set; }
        public Abbruchgrund Abbruchgrund { get; set; }
        public int Ausleihdauer { get; set; } = 90;
        public string MahndatumInfo { get; set; }
        public int AnzahlMahnungen { get; set; }
        public bool? Benutzungskopie { get; set; }
        public DateTime? DatumDerFreigabe { get; set; }
        public string SachbearbeiterId { get; set; }
        public bool HasAufbereitungsfehler { get; set; }
        public GebrauchskopieStatus GebrauchskopieStatus { get; set; }
    }

    public enum DigitalisierungsKategorie
    {
        Keine = 0,
        Spezial = 1,
        Intern = 2,
        Oeffentlichkeit = 3,
        Forschungsgruppe = 4, // z.B. DDS ist eine Forschungsgruppe
        Gesuch = 5,
        Termin = 6,
        Amt = 7
    }

    public enum GebrauchskopieStatus
    {
        NichtErstellt = 0,
        ErfolgreichErstellt = 1,
        Fehlgeschlagen = 2,
        Versendet = 3
    }
}