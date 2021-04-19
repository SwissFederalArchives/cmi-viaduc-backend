using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Order.Mails
{
    public class Verzoegerungsmeldung : EmailTemplate
    {
        [DefaultValue("bestellung@bar.admin.ch")]
        public override string From { get; set; }

        [DefaultValue("{{Besteller.EmailAddress}}")]
        public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("")] public override string Bcc { get; set; }

        [DefaultValue("{{#Besteller.IstDeutsch}}" +
                      "Ihre Bestellung vom " +
                      "{{/Besteller.IstDeutsch}}" +
                      "{{#Besteller.IstFranzösisch}}" +
                      "Votre commande du " +
                      "{{/Besteller.IstFranzösisch}}" +
                      "{{#Besteller.IstItalienisch}}" +
                      "Ordinazione del " +
                      "{{/Besteller.IstItalienisch}}" +
                      "{{#Besteller.IstEnglisch}}" +
                      "Your order of " +
                      "{{/Besteller.IstEnglisch}}" +
                      "{{Bestellung.Erfassungsdatum}}")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}