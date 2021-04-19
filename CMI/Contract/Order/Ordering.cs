using System;
using Newtonsoft.Json;

namespace CMI.Contract.Order
{
    public class Ordering
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public OrderType Type { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public OrderItem[] Items { get; set; }

        public string Comment { get; set; }
        public int? ArtDerArbeit { get; set; }
        public DateTime? LesesaalDate { get; set; }
        public DateTime? OrderDate { get; set; }
        public string BegruendungEinsichtsgesuch { get; set; }
        public Eingangsart Eingangsart { get; set; }
        public bool PersonenbezogeneNachforschung { get; set; }
        public bool HasEigenePersonendaten { get; set; }
        public string RolePublicClient { get; set; }
    }
}