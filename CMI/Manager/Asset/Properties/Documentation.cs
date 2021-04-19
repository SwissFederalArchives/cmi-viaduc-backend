using CMI.Utilities.Common;

namespace CMI.Manager.Asset.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.PasswordSeed,
                "Passwort Seed für die Generierung des Passwortes. Beispiel: Zeichenkette mit 1653 ganz unterschiedlichen Zeichen");
            AddDescription<Settings>(x => x.PickupPath, "Lokaler Pfad zum Vshell Verzeichnis");
            AddDescription<DbConnectionSetting>(x => x.ConnectionString, "ConnectionString zur Viaduc-DB");
        }
    }
}