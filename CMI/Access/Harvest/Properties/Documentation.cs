using CMI.Utilities.Common;

namespace CMI.Access.Harvest.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.ActaProEndpoint, "Der Endpunkt für das ActaPro API");
            AddDescription<Settings>(x => x.ActaProTokenEndpoint, "Der Endpunkt um ein Token für das ActaPro API zu erhalten");
            AddDescription<Settings>(x => x.ActaProTokenUser, "Der Username um ein Token zu erhalten");
            AddDescription<Settings>(x => x.ActaProTokenPassword, "Das Passwort um ein Token zu erhalten");
            AddDescription<Settings>(x => x.ActaProGrantType, "Gibt an, wie ActaPro das Zugriffstoken erhält");
            AddDescription<Settings>(x => x.ActaProUser, "Der Username um eine API Anfrage zu schicken");
            AddDescription<Settings>(x => x.ActaProPassword, "Das Passwort um eine API Anfrage zu schicken");
        }
    }
}