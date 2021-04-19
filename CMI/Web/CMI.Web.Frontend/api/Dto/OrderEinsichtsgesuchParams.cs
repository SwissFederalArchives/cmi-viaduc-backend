using CMI.Contract.Order;

namespace CMI.Web.Frontend.api.Dto
{
    public class OrderEinsichtsgesuchParams
    {
        public string UserId { get; set; }
        public OrderType Type { get; set; }
        public string Comment { get; set; }
        public int? ArtDerArbeit { get; set; }
        public bool PersonenbezogeneNachforschung { get; set; }
        public bool HasEigenePersonendaten { get; set; }
        public string BegruendungEinsichtsgesuch { get; set; }
    }
}