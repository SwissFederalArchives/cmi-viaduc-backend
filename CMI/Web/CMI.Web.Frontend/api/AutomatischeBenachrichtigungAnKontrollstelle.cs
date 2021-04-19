using System.ComponentModel;
using CMI.Contract.Parameter.AdditionalParameterTypes;
using CMI.Contract.Parameter.Attributes;

namespace CMI.Web.Frontend.api
{
    public class AutomatischeBenachrichtigungAnKontrollstelle : EmailTemplate
    {
        [DefaultValue("")] public override string From { get; set; }

        [DefaultValue("{{To}}")] public override string To { get; set; }

        [DefaultValue("")] public override string Cc { get; set; }

        [DefaultValue("")] public override string Bcc { get; set; }

        [DefaultValue("Automatische Benachrichtigung über Ausleihe von Unterlagen innerhalb Schutzfrist")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource] public override string Body { get; set; }
    }
}