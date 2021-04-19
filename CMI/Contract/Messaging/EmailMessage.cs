using System.Net.Mail;

namespace CMI.Contract.Messaging
{
    public class EmailMessage : IEmailMessage
    {
        public EmailMessage()
        {
            LogAllowed = true;
        }

        public string Bcc { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public string CC { get; set; }
        public MailPriority Priority { get; set; }
        public bool LogAllowed { get; set; }
    }
}