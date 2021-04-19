using System.Collections.Generic;
using CMI.Contract.Order;

namespace CMI.Web.Management.api.Data
{
    public class InVorlageExportierenParams
    {
        public List<int> OrderItemIds { get; set; }
        public Vorlage Vorlage { get; set; }
        public string Sprache { get; set; }
    }
}