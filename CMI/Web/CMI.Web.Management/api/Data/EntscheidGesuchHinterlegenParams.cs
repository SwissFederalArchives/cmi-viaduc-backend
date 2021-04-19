using System;
using System.Collections.Generic;
using CMI.Contract.Order;

namespace CMI.Web.Management.api.Data
{
    public class EntscheidGesuchHinterlegenParams
    {
        public List<int> OrderItemIds { get; set; }
        public EntscheidGesuch Entscheid { get; set; }
        public DateTime DatumEntscheid { get; set; }
        public string InterneBemerkung { get; set; }
    }
}