using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Vecteur
{
    public class DigipoolAufbereitungFehlgeschlagen : EmailTemplate
    {
        [DefaultValue("")] public override string From { get; set; }

        [DefaultValue("bestellung@bar.admin.ch")]
        public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("")] public override string Bcc { get; set; }

        [DefaultValue("Aufbereitungsfehler Digitalisierungsauftrag ({{#Aufträge}}{{Id}}){{/Aufträge}}")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}