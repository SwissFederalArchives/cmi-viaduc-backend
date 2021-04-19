using System.ComponentModel;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Web.Frontend.ParameterSettings
{
    public class DigitalisierungsbeschraenkungSettings : ISetting
    {
        [Description("Gibt an, wieviele aktive Aufträge ein Ö2 Benutzer gleichzeitig haben darf.")]
        [DefaultValue(3)]
        [Validation(@"^(?![0]$)\d{1,}$", "Geben Sie eine Zahl > 0 ein")]
        public int DigitalisierungsbeschraenkungOe2 { get; set; }

        [Description("Gibt an, wieviele aktive Aufträge ein Ö3 Benutzer gleichzeitig haben darf.")]
        [DefaultValue(3)]
        [Validation(@"^(?![0]$)\d{1,}$", "Geben Sie eine Zahl > 0 ein")]
        public int DigitalisierungsbeschraenkungOe3 { get; set; }

        [Description("Gibt an, wieviele aktive Aufträge ein BVW Benutzer gleichzeitig haben darf.")]
        [DefaultValue(3)]
        [Validation(@"^(?![0]$)\d{1,}$", "Geben Sie eine Zahl > 0 ein")]
        public int DigitalisierungsbeschraenkungBvw { get; set; }

        [Description("Gibt an, wieviele aktive Aufträge ein AS Benutzer gleichzeitig haben darf.")]
        [DefaultValue(3)]
        [Validation(@"^(?![0]$)\d{1,}$", "Geben Sie eine Zahl > 0 ein")]
        public int DigitalisierungsbeschraenkungAs { get; set; }

        [Description("Gibt an, wieviele aktive Aufträge ein BAR Benutzer gleichzeitig haben darf.")]
        [DefaultValue(3)]
        [Validation(@"^(?![0]$)\d{1,}$", "Geben Sie eine Zahl > 0 ein")]
        public int DigitalisierungsbeschraenkungBar { get; set; }
    }
}