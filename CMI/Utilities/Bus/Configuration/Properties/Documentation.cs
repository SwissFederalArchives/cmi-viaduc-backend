using CMI.Utilities.Common;

namespace CMI.Utilities.Bus.Configuration.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.RabbitMqPassword, "Passwort des RabbitMq");
            AddDescription<Settings>(x => x.RabbitMqUri, "URL des RabbitMq");
            AddDescription<Settings>(x => x.RabbitMqUriResponseAddress, "Antwortadresse des RabbitMq (Wird für den Zonenübergang benötigt)");
            AddDescription<Settings>(x => x.RabbitMqUserName, "Benutzer des RabbitMq");
            AddDescription<Settings>(x => x.PrefetchCountSettings, "Anzahl der gleichzeitig verarbeiteten Messages der konfigurierten Queues.");
        }
    }
}