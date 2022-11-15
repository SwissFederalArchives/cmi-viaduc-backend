using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Onboarding
{
    public class IdentifizierungAdminWarnung : EmailTemplate
    {
        [DefaultValue("noreply@bar.admin.ch")] 
        public override string From { get; set; }

        [DefaultValue("benutzer-admin@bar.admin.ch")]
        public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("")] public override string Bcc { get; set; }

        [DefaultValue("Identifizierung prüfen")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}