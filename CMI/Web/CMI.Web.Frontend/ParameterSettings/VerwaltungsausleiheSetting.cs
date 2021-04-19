using System.ComponentModel;
using CMI.Contract.Parameter;

namespace CMI.Web.Frontend.ParameterSettings
{
    public class VerwaltungsausleiheSettings : ISetting
    {
        [Description(
            "Fixwert für Feld 'Art der Arbeit' bei Bestellungen vom Typ Verwaltungsausleihe. Es muss der Code eingegeben werden (Tabelle 'ArtDerArbeit').")]
        [DefaultValue((long) 9)]
        public long ArtDerArbeitFuerAmtsBestellung { get; set; }
    }
}