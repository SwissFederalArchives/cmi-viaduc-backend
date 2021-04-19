using System.ComponentModel;


namespace CMI.Contract.Parameter
{
     /// <summary>
     /// Zur Konfiguration der Tageskontingente für Digitalisierungsaufträge
     /// </summary>
    public class KontingentDigitalisierungsauftraegeSetting : CentralizedSetting
    {
        [Description("Tageskontingente pro Benutzer für Digitalisierungsaufträge mit Digitalisierungskategorie Intern")]
        [Default(3)]
        [Validator(@"^[1-9][0-9]*$", "Geben Sie eine Ganzzahl ein, die grösser als null ist.")]
        public int Intern { get; set; }        
        
        [Description("Tageskontingente pro Benutzer für Digitalisierungsaufträge mit Digitalisierungskategorie Öeffentlichkeit")]
        [Default(1)]
        [Validator(@"^[1-9][0-9]*$", "Geben Sie eine Ganzzahl ein, die grösser als null ist.")]
        public int Oeffentlichkeit { get; set; } 

        [Description("Tageskontingente pro Benutzer für Digitalisierungsaufträge mit Digitalisierungskategorie Gesuch")]
        [Default(20)]
        [Validator(@"^[1-9][0-9]*$", "Geben Sie eine Ganzzahl ein, die grösser als null ist.")]
        public int Gesuch { get; set; } 

        [Description("Tageskontingente pro Benutzer für Digitalisierungsaufträge mit Digitalisierungskategorie Amt")]
        [Default(3)]
        [Validator(@"^[1-9][0-9]*$", "Geben Sie eine Ganzzahl ein, die grösser als null ist.")]
        public int Amt { get; set; } 
    }
}
