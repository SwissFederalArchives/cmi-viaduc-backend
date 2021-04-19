using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Order.Mails
{
    public class NeuesEinsichtsgesuch : EmailTemplate
    {
        [DefaultValue("")] public override string From { get; set; }

        [DefaultValue("einsichtsgesuch@bar.admin.ch")]
        public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("")] public override string Bcc { get; set; }

        [DefaultValue("Neues Einsichtsgesuch: {{User.Vorname}} {{User.Name}}, {{User.Organisation}}, {{User.Ort}} / {{Bestellung.OrderId}}")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}