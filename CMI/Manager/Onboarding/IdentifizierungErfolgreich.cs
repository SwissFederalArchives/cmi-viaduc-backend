using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Onboarding
{
    public class IdentifizierungErfolgreich : EmailTemplate
    {
        [DefaultValue("")] public override string From { get; set; }

        [DefaultValue("{{User.EmailAddress}}")]
        public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("benutzer-admin@bar.admin.ch")]
        public override string Bcc { get; set; }

        [DefaultValue("{{#User.IstDeutsch}}" +
                      "Identifizierung abgeschlossen" +
                      "{{/User.IstDeutsch}}" +
                      "{{#User.IstFranzösisch}}" +
                      "Votre identification est terminée" +
                      "{{/User.IstFranzösisch}}" +
                      "{{#User.IstItalienisch}}" +
                      "Identificazione riuscita" +
                      "{{/User.IstItalienisch}}" +
                      "{{#User.IstEnglisch}}" +
                      "Identification completed" +
                      "{{/User.IstEnglisch}}")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}