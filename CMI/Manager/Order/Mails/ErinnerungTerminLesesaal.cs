using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Manager.Order.Mails
{
    public class ErinnerungTerminLesesaal : EmailTemplate
    {
        [DefaultValue("bestellung@bar.admin.ch")]
        public override string From { get; set; }

        [DefaultValue("")] public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("bestellung@bar.admin.ch")]
        public override string Bcc { get; set; }

        [DefaultValue("{{#Sprachen}}" +
                      "{{#IstDeutsch}}" +
                      "Erinnerung Bereitstellung" +
                      "{{/IstDeutsch}}" +
                      "{{#IstFranzösisch}}" +
                      "Rappel réservation de documents" +
                      "{{/IstFranzösisch}}" +
                      "{{#IstItalienisch}}" +
                      "Documenti pronti per la consultazione – promemoria" +
                      "{{/IstItalienisch}}" +
                      "{{#IstEnglisch}}" +
                      "Reminder: Documents ready" +
                      "{{/IstEnglisch}}" +
                      "{{/Sprachen}}")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}
