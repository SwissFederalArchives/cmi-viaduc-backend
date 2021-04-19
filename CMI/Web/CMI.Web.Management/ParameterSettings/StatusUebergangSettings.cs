using System.ComponentModel;
using CMI.Contract.Parameter;

namespace CMI.Web.Management.ParameterSettings
{
    public class StatusUebergangSettings : ISetting
    {
        [Description(
            "Fixwert für Feld 'Art der Arbeit' bei der Funktion 'Digitalisierung auslösen' im Management-Client. Es muss der Code eingegeben werden (Tabelle 'ArtDerArbeit').")]
        [DefaultValue(9)]
        public int ArtDerArbeitIdFuerDigitalisierungAusloesen { get; set; }
    }
}