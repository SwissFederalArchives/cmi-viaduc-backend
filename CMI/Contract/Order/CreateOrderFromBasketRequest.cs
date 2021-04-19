using System;
using System.Collections.Generic;

namespace CMI.Contract.Order
{
    public class OrderCreationRequest
    {
        public List<int> OrderItemIdsToExclude { get; set; }
        public OrderType Type { get; set; }
        public string Comment { get; set; }
        public int? ArtDerArbeit { get; set; }
        public DateTime? LesesaalDate { get; set; }
        public string BegruendungEinsichtsgesuch { get; set; }
        public string CurrentUserId { get; set; }
        public string BestellerId { get; set; }
        public bool PersonenbezogeneNachforschung { get; set; }
        public bool HasEigenePersonendaten { get; set; }
        public bool FunktionDigitalisierungAusloesen { get; set; } = false;
    }
}