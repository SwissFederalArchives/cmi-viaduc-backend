using System.ComponentModel;
using CMI.Contract.Parameter;

namespace CMI.Manager.Asset.ParameterSettings
{
    public class SchaetzungAufbereitungszeitSettings : ISetting
    {
        [Description("Der Wert wird verwendet bei der Schätzung der Aufbereitungszeit für Gebrauchskopien mit Video. Einheit: KByte pro Sekunde")]
        [DefaultValue(1000)]
        public int KonvertierungsgeschwindigkeitVideo { get; set; }

        [Description(
            "Der Wert wird verwendet bei der Schätzung der Aufbereitungszeit für Gebrauchskopien mit Audio Dateie(en). Einheit: KByte pro Sekunde")]
        [DefaultValue(1000)]
        public int KonvertierungsgeschwindigkeitAudio { get; set; }

        [Description("Der Wert wird verwendet bei der Schätzung der Zeit für das Entzippen der Dateien, die bei der Synchronisierung nicht " +
                     "aus dem Repository geladen wurden. Einheit: KByte pro Sekunde. Default ist 30 MB/s")]
        [DefaultValue(30000)]
        public int DecompressionSpeedInKByte { get; set; }
    }
}