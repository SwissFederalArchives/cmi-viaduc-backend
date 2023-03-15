using System.ComponentModel;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Engine.Asset.ParameterSettings 
{
    public class ViewerConversionSettings : ISetting
    {
        [Description("Gibt vor, welche Qualität die eingefügten Bilder haben. Durch eine tiefere Qualität erreicht man eine kleinere Dateigrösse.")]
        [DefaultValue(80)]
        [Validation("^[1-9][0-9]?$|^100$", "Geben Sie eine Ganzzahl grösser 0 und kleiner gleich 100 ein.")]
        public int JpegQualitaetInProzent { get; set; }

        [Description("Default-Auflösung für resultierende JPEG, in dpi")]
        [DefaultValue(96)]
        public int DefaultAufloesungInDpi { get; set; }


    }
}
