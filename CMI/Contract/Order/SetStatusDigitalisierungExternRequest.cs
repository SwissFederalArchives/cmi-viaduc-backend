namespace CMI.Contract.Order
{
    public class SetStatusDigitalisierungExternRequest : ISingleOrderId
    {
        public int OrderItemId { get; set; }
    }
}