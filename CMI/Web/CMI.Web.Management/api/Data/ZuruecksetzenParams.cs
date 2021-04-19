using System.Collections.Generic;
using CMI.Contract.Order;

namespace CMI.Web.Management.api.Data
{
    public class ZuruecksetzenParams
    {
        public List<int> OrderItemIds { get; set; }
    }

    public class AbbrechenParams
    {
        public List<int> OrderItemIds { get; set; }
        public Abbruchgrund Abbruchgrund { get; set; }
        public string BemerkungZumDossier { get; set; }
        public string InterneBemerkung { get; set; }
    }

    public class AuftraegeAusleihenParams
    {
        public List<int> OrderItemIds { get; set; }
    }

    public class DigitalisierungAusloesenParams
    {
        public List<int> OrderItemIds { get; set; }
    }
}