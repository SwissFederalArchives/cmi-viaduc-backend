using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Asset.Mails
{
    public class GebrauchskopieErstellenProblem : EmailTemplate
    {
        [DefaultValue("bestellung@bar.admin.ch")]
        public override string From { get; set; }

        [DefaultValue("{{User.EmailAddress}}")]
        public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("")] public override string Bcc { get; set; }

        [DefaultValue("{{#User.IstDeutsch}}" +
                      "Aufbereitung fehlgeschlagen " +
                      "{{/User.IstDeutsch}}" +
                      "{{#User.IstFranzösisch}}" +
                      "Échec de la préparation des documents " +
                      "{{/User.IstFranzösisch}}" +
                      "{{#User.IstItalienisch}}" +
                      "Preparazione non riuscita " +
                      "{{/User.IstItalienisch}}" +
                      "{{#User.IstEnglisch}}" +
                      "Preparation failed " +
                      "{{/User.IstEnglisch}}" +
                      "({{Ve.Signatur}})")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}