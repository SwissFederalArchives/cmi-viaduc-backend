using System;
using System.Collections.Generic;
using CMI.Contract.Order;

namespace CMI.Web.Management.api.Data
{
    public class EntscheidFreigabeHinterlegenParams
    {
        public List<int> OrderItemIds { get; set; }
        public ApproveStatus Entscheid { get; set; }
        public DateTime? DatumBewilligung { get; set; }
        public string InterneBemerkung { get; set; }
    }
}