using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMI.Access.Harvest.ActaPro.Security
{
    public record SecurityCalculationInputData
    {
        public DateTime? SchutzfristEnde { get; set; }
        public string BearbeitungsStatus { get; set; }
        public string Zugaenglichkeit { get; set; }
        public bool MetadatenPublizierbar { get; set; }
        public string Stufe { get; set; }
        public string Schutzfristkategorie { get; set; }
        public DateTime? EntstehungszeitraumBis { get; set; }
        public string ZugaenglichkeitGemaessBga { get; set; }
        public List<string> ZustaendigeStellenKeys { get; set; } = new();
        public string Publikationsrechte { get; set; }
        public string DocKey { get; set; }
        public string SynchronisationOnlineZugang { get; set; }
    }
}
