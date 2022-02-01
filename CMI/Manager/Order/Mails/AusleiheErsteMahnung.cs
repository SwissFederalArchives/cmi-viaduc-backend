using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Order.Mails
{
    public class AusleiheErsteMahnung : EmailTemplate
    {
        [DefaultValue("bestellung@bar.admin.ch")]
        public override string From { get; set; }

        [DefaultValue("")] public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("bestellung@bar.admin.ch")]
        public override string Bcc { get; set; }

        [DefaultValue("{{#Sprachen}}" +
                      "{{#IstDeutsch}}" +
                      "Erste Mahnung Verwaltungsausleihe" +
                      "{{/IstDeutsch}}" +
                      "{{#IstFranzösisch}}" +
                      "Prêt à l’administration : premier rappel" +
                      "{{/IstFranzösisch}}" +
                      "{{/Sprachen}}")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}