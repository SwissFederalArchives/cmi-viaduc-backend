using System;
using CMI.Contract.Order;

namespace CMI.Access.Sql.Viaduc
{
    public class UpdateOrderingParams
    {
        public OrderType Type { get; set; }
        public string Comment { get; set; }
        public int? ArtDerArbeit { get; set; }
        public DateTime? LesesaalDate { get; set; }
        public string UserId { get; set; }
        public string BegruendungEinsichtsgesuch { get; set; }
        public bool PersonenbezogeneNachforschung { get; set; }
        public bool HasEigenePersonendaten { get; set; }
    }
}