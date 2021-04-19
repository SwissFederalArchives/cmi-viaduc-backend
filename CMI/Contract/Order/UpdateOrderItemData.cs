using System;

namespace CMI.Contract.Order
{
    public class UpdateOrderItemData
    {
        public int Id { get; set; }
        public string Comment { get; set; }
        public DateTime? BewilligungsDatum { get; set; }
        public bool HasPersonendaten { get; set; }
        public int? Reason { get; set; }
        public int DigitalisierungsKategorie { get; set; }
        public DateTime? TerminDigitalisierung { get; set; }
        public string InternalComment { get; set; }
        public int Ausleihdauer { get; set; } = 90;
        public string MahndatumInfo { get; set; }
        public int AnzahlMahnungen { get; set; }
        public int GebrauchskopieStatus { get; set; }
    }
}