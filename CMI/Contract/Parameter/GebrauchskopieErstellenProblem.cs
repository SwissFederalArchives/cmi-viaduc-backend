using System.ComponentModel;

namespace CMI.Contract.Parameter
{
    public class GebrauchskopieErstellenProblem : EmailTemplateSetting
    {
        [Default("")]
        [Description("Wenn leer wird der Wert aus dem Parameter-Service 'NotificationSettings' gelesen")]
        public override string From { get; set; }

        [Default("{{User.EmailAddress}}")]
        public override string To { get; set; }

        [Default("")]
        public override string CC { get; set; }

        [Default("")]
        public override string Bcc { get; set; }

        [Default("Problem mit Unterlagen")]
        public override string Subject { get; set; }

        [ReadDefaultFromResource]
        public override string Body { get; set; }
    }
}
