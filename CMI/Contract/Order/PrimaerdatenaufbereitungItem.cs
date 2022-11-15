using System;

namespace CMI.Contract.Order
{
    public  class PrimaerdatenAufbereitungItem
    {
        public int? OrderingType { get; set; }
        public int? OrderItemId { get; set; }
        public string Dossiertitel { get; set; }
        public int? VeId { get; set; }
        public string Signatur { get; set; }
        public DateTime? NeuEingegangen { get; set; }
        public DateTime? Ausgeliehen { get; set; }
        public DateTime? ZumReponierenBereit { get; set; }
        public DateTime? Abgeschlossen { get; set; }
        public DateTime? AuftragErledigt { get; set; }
        public int? PrimaerdatenAuftragId { get; set; }
        public long? GroesseInBytes { get; set; }
        public string AufbereitungsArt { get; set; }
        public string PackageMetadata { get; set; }
        public int? DigitalisierungsKategorieId { get; set; }
    }
}
