using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Asset.Mails
{
    public class GebrauchskopiePasswort : EmailTemplate
    {
        [DefaultValue("bestellung@bar.admin.ch")]
        public override string From { get; set; }

        [DefaultValue("{{User.EmailAddress}}")]
        public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("")] public override string Bcc { get; set; }

        [DefaultValue("{{#User.IstDeutsch}}" +
                      "Passwort" +
                      "{{/User.IstDeutsch}}" +
                      "{{#User.IstFranzösisch}}" +
                      "Mot de passe" +
                      "{{/User.IstFranzösisch}}" +
                      "{{#User.IstItalienisch}}" +
                      "Parola d'accesso" +
                      "{{/User.IstItalienisch}}" +
                      "{{#User.IstEnglisch}}" +
                      "Password" +
                      "{{/User.IstEnglisch}}" +
                      " ({{Ve.Signatur}})")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}