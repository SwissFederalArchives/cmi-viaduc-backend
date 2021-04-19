using System;

namespace CMI.Contract.Order
{
    public class OrderExecutedWaitList
    {
        public int OrderExecutedWaitListId { get; set; }
        public int VeId { get; set; }
        public bool Processed { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string SerializedMessage { get; set; }
        public DateTime InsertedOn { get; set; }
    }
}