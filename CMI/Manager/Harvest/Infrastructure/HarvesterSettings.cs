using System.ComponentModel;
using CMI.Contract.Parameter;

namespace CMI.Manager.Harvest.Infrastructure
{
    public class HarvesterSettings : ISetting
    {
        [Description("Gibt an, ob bei der Synchronisierung der Primärdaten-Volltext erneut erstellt werden soll, oder dieser ggf. aus dem " +
                     "vorhandenen indizierten Datensatz übernommen werden soll. Standard ist 'false'. Eine Änderung des Wertes wird nach spätestens 20 Minuten aktiv.")]
        [DefaultValue(false)]
        public bool EnableFullResync { get; set; }
    }
}