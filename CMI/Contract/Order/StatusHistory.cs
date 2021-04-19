using System;

namespace CMI.Contract.Order
{
    public class StatusHistory
    {
        public int Id { get; set; }
        public int OrderItemId { get; set; }
        public DateTime StatusChangeDate { get; set; }
        public OrderStatesInternal FromStatus { get; set; }
        public OrderStatesInternal ToStatus { get; set; }
        public string ChangedBy { get; set; }
    }
}