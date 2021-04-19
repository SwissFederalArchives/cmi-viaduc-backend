using System.Net.Mail;

namespace CMI.Contract.Messaging
{
    public interface IEmailMessage
    {
        string From { get; set; }
        string To { get; set; }
        string CC { get; set; }
        string Bcc { get; set; }

        string Subject { get; set; }

        /// <summary>Im HTML Format</summary>
        string Body { get; set; }

        MailPriority Priority { get; set; }

        bool LogAllowed { get; set; }
    }
}