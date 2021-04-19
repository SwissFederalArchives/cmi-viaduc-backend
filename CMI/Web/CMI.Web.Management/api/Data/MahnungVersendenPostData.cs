using System.Collections.Generic;

namespace CMI.Web.Management.api.Data
{
    public class MahnungVersendenPostData
    {
        public List<int> OrderItemIds { get; set; }
        public string Language { get; set; }
        public int GewaehlteMahnungAnzahl { get; set; }
    }
}