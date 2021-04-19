using System.ComponentModel;
using CMI.Contract.Parameter;

namespace CMI.Manager.Repository.ParameterSettings
{
    public class RepositorySyncSettings : ISetting
    {
        [Description("Komma separierte Liste mit Dateitypen, die bei der Synchronisierung nicht aus dem DIR exportiert werden müssen. " +
                     "Die Dateiendung muss in der Form \".mp3\" angegeben werden.")]
        [DefaultValue(".mp3, .avi, .mp4, .wav")]
        public string IgnorierteDateitypenFuerSynchronisierung { get; set; }

        [Description("Der Wert wird verwendet bei der Schätzung der Downloadzeit für Dateien, die bei der Synchronisierung nicht " +
                     "aus dem Repository geladen wurden. Einheit: KByte pro Sekunde. Default ist 25 MB/s")]
        [DefaultValue(25000)]
        public int RepositoryDownloadSpeedInKByte { get; set; }

        [Description("Der Wert wird verwendet bei der Schätzung der Zeit für das Zippen der Dateien, die bei der Synchronisierung nicht " +
                     "aus dem Repository geladen wurden. Einheit: KByte pro Sekunde. Default ist 15 MB/s")]
        [DefaultValue(15000)]
        public int CompressionSpeedInKByte { get; set; }

        [Description("Der Wert wird verwendet bei der Schätzung der Zeit für das Transferieren der Dateien, die bei der Synchronisierung nicht " +
                     "aus dem Repository geladen wurden. Einheit: KByte pro Sekunde. Default ist 100 MB/s")]
        [DefaultValue(100000)]
        public int FileTransferSpeedInKByte { get; set; }

        [Description("Liste mit regulären Ausdrücken anhand derer Dateien aus dem Repository ignoriert werden. Pro Zeile ein Ausdruck." +
                     "Vergleich erfolgt mit dem Dateinamen. Gross- und Kleinschreibung wird ignoriert.")]
        [DefaultValue(".*_BARDIGI.xml")]
        public string IgnorierteDateinamenRegex { get; set; }
    }
}