using System;

namespace CMI.Contract.Order
{
    public  class PrimaerdatenAufbereitungItem
    {
        public DateTime? OrderingDate { get; set; }
        public int? OrderingType { get; set; }
        public int? OrderItemId { get; set; }
        public string Dossiertitel { get; set; }
        public int? VeId { get; set; }
        public int? MutationsId { get; set; }
        public DateTime? NeuEingegangen { get; set; }
        public DateTime? FreigabePruefen { get; set; }
        public DateTime? FuerDigitalisierungBereit { get; set; }
        public DateTime? FuerAushebungBereit { get; set; }
        public DateTime? Ausgeliegen { get; set; }
        public DateTime? ZumReponierenBereit { get; set; }
        public DateTime? Abgeschlossen { get; set; }
        public DateTime? Abgebrochen { get; set; }
        public int? PrimaerdatenAuftragId { get; set; }
        public string AufbereitungsArt { get; set; }
        public long? GroesseInBytes { get; set; }
        public string Quelle { get; set; }
        public int? GeschaetzteAufbereitungszeit { get; set; }
        public DateTime? Registriert { get; set; }
        public DateTime? LetzterAufbereitungsversuch { get; set; }
        public DateTime? ErsterAufbereitungsversuch { get; set; }
        public DateTime? AuftragErledigt { get; set; }
        public DateTime? ImCacheAbgelegt { get; set; }
        public int? AnzahlVersucheDownload { get; set; }
        public int? AnzahlVersucheSync { get; set; }
        public int? Verarbeitungskanal { get; set; }
        public string PackageMetadata { get; set; }
    }
}
