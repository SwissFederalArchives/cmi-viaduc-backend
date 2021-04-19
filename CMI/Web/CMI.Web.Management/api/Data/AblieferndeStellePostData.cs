using System.Collections.Generic;

namespace CMI.Web.Management.api.Data
{
    public class AblieferndeStellePostData
    {
        public int AblieferndeStelleId { get; set; }

        public string Bezeichnung { get; set; }

        public string Kuerzel { get; set; }

        public List<int> TokenIdList { get; set; }

        public List<string> Kontrollstellen { get; set; }
    }
}