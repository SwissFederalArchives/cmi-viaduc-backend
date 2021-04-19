using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Order.Mails
{
    public class DigitalisierungsAuftragErledigtProblem : EmailTemplate
    {
        [DefaultValue("")] public override string From { get; set; }

        [DefaultValue("bestellung@bar.admin.ch")]
        public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("")] public override string Bcc { get; set; }

        [DefaultValue("Erstellung Gebrauchskopie nach Abschluss Digitalisierung nicht möglich " +
                      "({{Ve.Signatur}})")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}