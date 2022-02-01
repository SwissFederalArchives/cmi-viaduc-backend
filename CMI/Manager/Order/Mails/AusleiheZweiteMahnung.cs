using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Order.Mails
{
    public class AusleiheZweiteMahnung : EmailTemplate
    {
        [DefaultValue("bestellung@bar.admin.ch")]
        public override string From { get; set; }

        [DefaultValue("")] public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("")] public override string Bcc { get; set; }

        [DefaultValue("{{#Sprachen}}" +
                      "{{#IstDeutsch}}" +
                      "Zweite Mahnung Verwaltungsausleihe" +
                      "{{/IstDeutsch}}" +
                      "{{#IstFranzösisch}}" +
                      "Prêt à l’administration : deuxième rappel" +
                      "{{/IstFranzösisch}}" +
                      "{{/Sprachen}}")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}