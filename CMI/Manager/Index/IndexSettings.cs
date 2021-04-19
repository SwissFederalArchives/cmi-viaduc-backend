using System.ComponentModel;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Index
{
    public class IndexSettings : ISetting
    {
        [Description("Gibt an, wie viele Tage ein Logindex im Elasticsearch aufbewahrt wird")]
        [DefaultValue(30)]
        [Validation(@"^[1-9][0-9]*$", "Geben Sie eine Ganzzahl ein, die grösser als null ist.")]
        public int AufbewahrungsdauerLogIndex { get; set; }
    }
}