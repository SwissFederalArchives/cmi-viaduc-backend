using System.ComponentModel;
using CMI.Contract.Parameter;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Cache
{
    public class CacheSettings : ISetting
    {
        [Description("Gibt die Aufbewahrungsfrist an für öffentliche Gebrauchskopien")]
        [Validation(@"\d+[ydhms]|infinite",
            "Geben Sie z.B. 'infinite', '8y', '3d', '7h', '15m' oder '30s' ein, um Die Dateien unbeschränkt, 8 Jahre, 3 Tage, 7 Stunden, 15 Minuten oder 30 Sekunden im Cache zu behalten.")]
        [DefaultValue("infinite")]
        public string RetentionSpanUsageCopyPublic { get; set; }

        [Description("Gibt die Aufbewahrungsfrist an für Gebrauchskopien unter Schutzfrist mit Einsichtsbewilligung (EB)")]
        [Validation(@"\d+[ydhms]|infinite",
            "Geben Sie z.B. 'infinite', '8y', '3d', '7h', '15m' oder '30s' ein, um Die Dateien unbeschränkt, 8 Jahre, 3 Tage, 7 Stunden, 15 Minuten oder 30 Sekunden im Cache zu behalten.")]
        [DefaultValue("120h")]
        public string RetentionSpanUsageCopyEb { get; set; }

        [Description("Gibt die Aufbewahrungsfrist an für Gebrauchskopien unter Schutzfrist mit Auskunftsgesuchsbewilligung (AB)")]
        [Validation(@"\d+[ydhms]|infinite",
            "Geben Sie z.B. 'infinite', '8y', '3d', '7h', '15m' oder '30s' ein, um Die Dateien unbeschränkt, 8 Jahre, 3 Tage, 7 Stunden, 15 Minuten oder 30 Sekunden im Cache zu behalten.")]
        [DefaultValue("12h")]
        public string RetentionSpanUsageCopyAb { get; set; }

        [Description("Gibt die Aufbewahrungsfrist an für Gebrauchskopien unter Schutzfrist mit für BAR- und AS-Benutzer")]
        [Validation(@"\d+[ydhms]|infinite",
            "Geben Sie z.B. 'infinite', '8y', '3d', '7h', '15m' oder '30s' ein, um Die Dateien unbeschränkt, 8 Jahre, 3 Tage, 7 Stunden, 15 Minuten oder 30 Sekunden im Cache zu behalten.")]
        [DefaultValue("12h")]
        public string RetentionSpanUsageCopyBarOrAS { get; set; }

        [Description("Gibt die Aufbewahrungsfrist an für Gebrauchskopien, die aus einer Benutzungskopie erstellt wurden")]
        [Validation(@"\d+[ydhms]|infinite",
            "Geben Sie z.B. 'infinite', '8y', '3d', '7h', '15m' oder '30s' ein, um Die Dateien unbeschränkt, 8 Jahre, 3 Tage, 7 Stunden, 15 Minuten oder 30 Sekunden im Cache zu behalten.")]
        [DefaultValue("90d")]
        public string RetentionSpanUsageCopyBenutzungskopie { get; set; }


        [Description(
            "Gibt die Gesamtgrösse des Caches an (in Gigabytes), bei deren Überschreitung das System täglich eine EMail an den Applikationsowner sendet.")]
        [DefaultValue((long) 512)]
        public long WarningThresholdCacheSize { get; set; }

        [Description(
            "Gibt die EMailadresse an, an welche das System Warnungen schickt, wenn die Grösse des Caches die WarningThresholdCacheSize überschreitet.")]
        [DefaultValue("marco.majoleth@bar.admin.ch; christa.ackermann@bar.admin.ch")]
        public string MailRecipient { get; set; }
    }
}