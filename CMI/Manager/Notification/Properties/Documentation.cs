using CMI.Utilities.Common;

namespace CMI.Manager.Notification.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<NotificationSettings>(x => x.DefaultFromAddress,
                "Absenderadresse der Mails, falls beim Mail selber kein Absender konfiguriert ist");
            AddDescription<NotificationSettings>(x => x.Host, "Adresse des Mailsservers");
            AddDescription<NotificationSettings>(x => x.Password, "Passwort des Mailsservers");
            AddDescription<NotificationSettings>(x => x.Port, "Port des Mailsservers");
            AddDescription<NotificationSettings>(x => x.StartupTestBody, "Nachricht des Mails für die Infomail beim Starten des Dienstes.");
            AddDescription<NotificationSettings>(x => x.StartupTestSubject, "Betreff des Mails für die Infomail beim Starten des Dienstes.");
            AddDescription<NotificationSettings>(x => x.StartupTestTo, "Empfänger des Mails für die Infomail beim Starten des Dienstes.");
            AddDescription<NotificationSettings>(x => x.UserName, "Benuzter des Mailservers");
        }
    }
}