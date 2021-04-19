using System.ComponentModel;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Asset.ParameterSettings
{
    public class GebrauchskopieSettings : ISetting
    {
        [Description(
            "Die vom Viaduc erstellten Gebrauchskopien enthalten eine Datei mit dem Namen 'readme.txt'. Der Inhalt dieser Datei kann hier festgelegt werden.")]
        [ReadDefaultFromResource]
        public string ReadmeDateiinhalt { get; set; }
    }
}