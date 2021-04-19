using System.Collections.Generic;

namespace CMI.Contract.Order
{
    public class OrderItemByUser
    {
        public string UserId { get; set; }
        public List<int> OrderItemIds { get; set; }
    }
}