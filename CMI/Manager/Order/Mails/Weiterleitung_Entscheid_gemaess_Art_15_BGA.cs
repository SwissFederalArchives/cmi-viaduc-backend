using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Order.Mails
{
    // ReSharper disable once InconsistentNaming
    public class Weiterleitung_Entscheid_gemaess_Art_15_BGA : EmailTemplate
    {
        [DefaultValue("")] public override string From { get; set; }

        [DefaultValue("{{User.EmailAddress}}")]
        public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("")] public override string Bcc { get; set; }

        [DefaultValue("{{#Sprachen}}" +
                      "{{#IstDeutsch}}" +
                      "Ihr Auskunftsgesuch vom " +
                      "{{/IstDeutsch}}" +
                      "{{#IstFranzösisch}}" +
                      "Votre demande de renseignement du " +
                      "{{/IstFranzösisch}}" +
                      "{{#IstItalienisch}}" +
                      "Richiesta di informazioni del " +
                      "{{/IstItalienisch}}" +
                      "{{#IstEnglisch}}" +
                      "Your information request of " +
                      "{{/IstEnglisch}}" +
                      "{{/Sprachen}}" +
                      "{{Erfassungsdatum}} ({{TeilBestand}})")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}