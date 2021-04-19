using System;

namespace CMI.Contract.Order
{
    public class UpdateOrderingData
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string Comment { get; set; }
        public int? ArtDerArbeit { get; set; }
        public DateTime LesesaalDate { get; set; }
        public string BegruendungEinsichtsgesuch { get; set; }
        public bool PersonenbezogeneNachforschung { get; set; }
        public bool HasEigenePersonendaten { get; set; }
    }
}