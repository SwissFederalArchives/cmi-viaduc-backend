using System;
using CMI.Contract.Order;
using Newtonsoft.Json;

namespace CMI.Web.Frontend.api.Dto
{
    public class OrderingDto
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public OrderType Type { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public OrderItemDto[] Items { get; set; }

        public string Comment { get; set; }
        public int? ArtDerArbeit { get; set; }
        public DateTime? LesesaalDate { get; set; }
        public DateTime? OrderDate { get; set; }
        public string BegruendungEinsichtsgesuch { get; set; }
        public bool PersonenbezogeneNachforschung { get; set; }
        public bool HasEigenePersonendaten { get; set; }
        public string RolePublicClient { get; set; }
    }
}