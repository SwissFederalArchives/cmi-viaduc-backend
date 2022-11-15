using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Onboarding
{
    public class IdentifizierungWarnung : EmailTemplate
    {
        [DefaultValue("noreply@bar.admin.ch")] 
        public override string From { get; set; }

        [DefaultValue("{{User.EmailAddress}}")]
        public override string To { get; set; }

        [DefaultValue("")] 
        public override string Cc { get; set; }

        [DefaultValue("")]
        public override string Bcc { get; set; }

        [DefaultValue("{{#User.IstDeutsch}}" +
                      "Prüfung Ihrer Identifizierung" +
                      "{{/User.IstDeutsch}}" +
                      "{{#User.IstFranzösisch}}" +
                      "Vérification de votre identification" +
                      "{{/User.IstFranzösisch}}" +
                      "{{#User.IstItalienisch}}" +
                      "Verifica della sua identificazione" +
                      "{{/User.IstItalienisch}}" +
                      "{{#User.IstEnglisch}}" +
                      "Verification of your identification" +
                      "{{/User.IstEnglisch}}")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}