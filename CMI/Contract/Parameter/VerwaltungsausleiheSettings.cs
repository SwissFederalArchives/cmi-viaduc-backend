using System.ComponentModel;

namespace CMI.Contract.Parameter
{
    public class VerwaltungsausleiheSettings : CentralizedSetting
    {
        [Description("Fixwert für Feld 'Art der Arbeit' bei Bestellungen vom Typ Verwaltungsausleihe. Es muss der Code eingegeben werden (Tabelle 'ArtDerArbeit').")]
        [Default(9)]
        public int ArtDerArbeitFuerAmtsBestellung { get; set; }
    }
}