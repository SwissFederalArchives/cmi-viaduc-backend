using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Order
{
    public class EinsichtsgesuchEmpfangsbestaetigung : EmailTemplate
    {
        [DefaultValue("bestellung@bar.admin.ch")]
        public override string From { get; set; }

        [DefaultValue("{{User.EmailAddress}}")]
        public override string To { get; set; }

        [DefaultValue("")]
        public override string Cc { get; set; }

        [DefaultValue("")]
        public override string Bcc { get; set; }

        [DefaultValue("{{#User.IstDeutsch}}Eingangsbestätigung Einsichtsgesuch{{/User.IstDeutsch}}{{#User.IstFranzösisch}}fr:Eingangsbestätigung Einsichtsgesuch{{/User.IstFranzösisch}}{{#User.IstItalienisch}}it:Eingangsbestätigung Einsichtsgesuch{{/User.IstItalienisch}}{{#User.IstEnglisch}}en:Eingangsbestätigung Einsichtsgesuch{{/User.IstEnglisch}}")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource]
        public override string Body { get; set; }
    }
}