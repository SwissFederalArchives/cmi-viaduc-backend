
using System.ComponentModel;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Engine.Asset.ParameterSettings
{
    public class AssetPreparationSettings : ISetting
    {
        [Description("Maximale Höhe oder Breite der gescannten Dateien in mm, damit noch eine OCR Erkennung gemacht wird")]
        [DefaultValue(430)]
        [Validation("^\\d{3,4}?$", "Geben Sie eine Ganzzahl grösser gleich 100 und kleiner 10000 ein.")]
        public int SizeThreshold { get; set; }

        [Description("Maximaler Speicherplatz eines Bildes im Verhältnis zu einem A4 in KB")]
        [DefaultValue(5000)]
        [Validation("^\\d{4,5}?$", "Geben Sie eine Ganzzahl grösser gleich 1000 und kleiner 100000 ein.")]
        public int MaxSizePerA4 { get; set; }

        [Description("Gibt vor, welche Qualität die eingefügten Bilder optimiert werden.Durch eine tiefere Qualität erreicht man eine kleinere Dateigrösse.")]
        [DefaultValue(80)]
        [Validation("^[1-9][0-9]?$|^100$", "Geben Sie eine Ganzzahl grösser 0 und kleiner gleich 100 ein.")]
        public int OptimizedQualityInPercent { get; set; }

        [Description("Gibt vor, wie viele Seiten in einem PDF prozentual zu grosse Bilder haben dürfen.")]
        [DefaultValue(10)]
        [Validation("^[1-9][0-9]?$|^100$", "Geben Sie eine Ganzzahl grösser 0 und kleiner gleich 100 ein.")]
        public int  AllowTooBigPercentage { get; set; }
    }
}
