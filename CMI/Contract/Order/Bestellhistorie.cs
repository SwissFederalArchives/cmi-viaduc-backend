using System;

namespace CMI.Contract.Order
{
    public class Bestellhistorie
    {
        public int AuftragsId { get; set; }
        public OrderType AuftragsTyp { get; set; }
        public ApproveStatus Freigabestatus { get; set; }
        public DateTime? DatumDerFreigabe { get; set; }
        public EntscheidGesuch EntscheidGesuch { get; set; }
        public DateTime? DatumDesEntscheids { get; set; }
        public string Besteller { get; set; }
        public string InterneBemerkung { get; set; }
        public string Sachbearbeiter { get; set; }
    }
}