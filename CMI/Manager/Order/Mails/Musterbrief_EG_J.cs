using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Order.Mails
{
    // ReSharper disable once InconsistentNaming
    public class Musterbrief_EG_J : EmailTemplate
    {
        [DefaultValue("")] public override string From { get; set; }

        [DefaultValue("{{User.EmailAddress}}")]
        public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("")] public override string Bcc { get; set; }

        [DefaultValue("{{#Sprachen}}" +
                      "{{#IstDeutschOderEnglisch}}" +
                      "Einsichtsgesuch von " +
                      "{{/IstDeutschOderEnglisch}}" +
                      "{{#IstFranzösisch}}" +
                      "Demande de consultation de " +
                      "{{/IstFranzösisch}}" +
                      "{{#IstItalienisch}}" +
                      "Domanda di consultazione di " +
                      "{{/IstItalienisch}}" +
                      "{{/Sprachen}}" +
                      "{{Besteller.Vorname}}" +
                      "{{Besteller.Name}} " +
                      "({{TeilBestand}})" +
                      "{{#Sprachen}}" +
                      "{{#IstDeutschOderEnglisch}} vom {{/IstDeutschOderEnglisch}}" +
                      "{{#IstFranzösisch}} du {{/IstFranzösisch}}" +
                      "{{#IstItalienisch}} del {{/IstItalienisch}}" +
                      "{{/Sprachen}}" +
                      "{{Erfassungsdatum}}")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}