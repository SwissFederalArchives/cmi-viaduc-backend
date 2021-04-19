using System.Collections.Generic;

namespace CMI.Contract.Order
{
    public class SetStatusZumReponierenBereitRequest
    {
        public List<int> OrderItemIds { get; set; }
        public string UserId { get; set; }
    }
}