namespace CMI.Contract.Order
{
    public class SetStatusDigitalisierungAbgebrochenRequest : ISingleOrderId
    {
        public string Grund { get; set; }
        public int OrderItemId { get; set; }
    }
}