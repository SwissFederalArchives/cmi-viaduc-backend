namespace CMI.Contract.Order
{
    public class SetStatusAushebungBereitRequest : ISingleOrderId
    {
        public int OrderItemId { get; set; }
    }
}