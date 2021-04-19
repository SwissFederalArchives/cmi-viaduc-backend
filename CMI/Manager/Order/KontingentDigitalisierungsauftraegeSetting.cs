using System.ComponentModel;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Order
{
    /// <summary>
    ///     Zur Konfiguration der Tageskontingente für Digitalisierungsaufträge
    /// </summary>
    public class KontingentDigitalisierungsauftraegeSetting : ISetting
    {
        public const string Regex = @"(^[1-9][0-9]*)\s\b(Aufträge|Auftrag)\sin\s([1-9][0-9]*)\s\b(Tagen|Tag)";

        [Description("Tageskontingent pro Benutzer für Digitalisierungsaufträge mit Digitalisierungskategorie Intern")]
        [DefaultValue("3 Aufträge in 3 Tagen")]
        [Validation(Regex, "Geben Sie jeweils eine Ganzzahl ein, die grösser als null ist (z.B. 5 Aufträge in 3 Tagen)")]
        public string Intern { get; set; }

        [Description("Tageskontingent pro Benutzer für Digitalisierungsaufträge mit Digitalisierungskategorie Öeffentlichkeit")]
        [DefaultValue("1 Auftrag in 3 Tagen")]
        [Validation(Regex, "Geben Sie jeweils eine Ganzzahl ein, die grösser als null ist (z.B. 1 Auftrag in 3 Tagen)")]
        public string Oeffentlichkeit { get; set; }

        [Description("Tageskontingent pro Benutzer für Digitalisierungsaufträge mit Digitalisierungskategorie Gesuche")]
        [DefaultValue("20 Aufträge in 3 Tagen")]
        [Validation(Regex, "Geben Sie jeweils eine Ganzzahl ein, die grösser als null ist (z.B. 5 Aufträge in 3 Tagen)")]
        public string Gesuche { get; set; }

        [Description("Tageskontingent pro Benutzer für Digitalisierungsaufträge mit Digitalisierungskategorie Amt")]
        [DefaultValue("3 Aufträge in 3 Tagen")]
        [Validation(Regex, "Geben Sie jeweils eine Ganzzahl ein, die grösser als null ist (z.B. 5 Aufträge in 3 Tagen)")]
        public string Amt { get; set; }

        // None Camel Case because it is written as is in the GUI and Camle Case would be wrong.
        [Description("Tageskontingent pro Benutzer für Digitalisierungsaufträge mit Digitalisierungskategorie DDS")]
        [DefaultValue("1 Auftrag in 3 Tagen")]
        [Validation(Regex, "Geben Sie jeweils eine Ganzzahl ein, die grösser als null ist (z.B. 1 Auftrag in 3 Tagen)")]
        public string DDS { get; set; }
    }
}