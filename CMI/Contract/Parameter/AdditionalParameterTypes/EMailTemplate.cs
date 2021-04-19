using System.ComponentModel;

namespace CMI.Contract.Parameter.AdditionalParameterTypes
{
    public abstract class EmailTemplate : ISetting
    {
        [Description("Gibt den Absender der E-Mails an. Wenn leer, wird der Absender aus den 'NotificationSettings' gelesen.")]
        public abstract string From { get; set; }

        [Description("Gibt den Empfänger der E-Mails an")]
        public abstract string To { get; set; }

        [Description("Gibt an, wer das E-Mail als CC erhält.")]
        public abstract string Cc { get; set; }

        [Description("Gibt an, wer das E-Mail als BCC erhält.")]
        public abstract string Bcc { get; set; }

        [Description("Geben Sie die Betreffzeile der E-Mail an.")]
        public abstract string Subject { get; set; }

        [Description("Geben Sie den Inhalt des E-Mails an. Sie können hier HTML verwenden.")]
        public abstract string Body { get; set; }
    }
}