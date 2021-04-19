using System.ComponentModel;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Asset.ParameterSettings
{
    public class ScansZusammenfassenSettings : ISetting
    {
        [Description(
            "Gibt vor, welche Auflösung die eingefügten Bilder haben, in Prozent. Die Einstellung 75 bedeutet zum Beispiel, dass ein Originalbild mit 100 x 100 Pixel im PDF noch eine Auflösung von 75 x 75 Pixel haben wird.")]
        [DefaultValue(100)]
        [Validation("^[1-9][0-9]?$|^100$", "Geben Sie eine Ganzzahl grösser 0 und kleiner gleich 100 ein.")]
        public int GroesseInProzent { get; set; }

        [Description("Gibt vor, welche Qualität die eingefügten Bilder haben. Durch eine tiefere Qualität erreicht man eine kleinere Dateigrösse.")]
        [DefaultValue(80)]
        [Validation("^[1-9][0-9]?$|^100$", "Geben Sie eine Ganzzahl grösser 0 und kleiner gleich 100 ein.")]
        public int JpegQualitaetInProzent { get; set; }

        [Description("Default-Auflösung für JPG2000 Dateien, wenn nicht durch PREMIS Datei ermittelbar, in dpi")]
        [DefaultValue(300)]
        public int DefaultAufloesungInDpi { get; set; }
    }
}