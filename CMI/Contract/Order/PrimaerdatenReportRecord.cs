using System;

namespace CMI.Contract.Order
{
    public class PrimaerdatenReportRecord
    {
        public int? OrderId { get; set; }
        public int? VeId { get; set; }
        public string Signatur { get; set; }
        public long Size { get; set; }
        public int FileCount { get; set; }
        public string DigitalisierungsKategorie { get; set; }
        public DateTime? NeuEingegangen { get; set; }
        public DateTime? Ausgeliehen { get; set; }
        public DateTime? ZumReponierenBereit { get; set; }
        public DateTime? Mail { get; set; }
        public string WartezeitDigitalisierung { get; set; }
        public string DauerDigitalisierung { get; set; }
        public string DigitalisierungTotal { get; set; }
        public string DauerAufbereitung { get; set; }
        public string TotalWartezeitNutzer { get; set; }
    }
}