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
            AddDescription<Settings>(x => x.SftpLicenseKey, "Der Lizenzschlüssel für den Rebex Sftp Server.");
            AddDescription<Settings>(x => x.TextExtractParallelism, "Anzahl gleichzeitig verarbeiteter Dokumente bei der Synchronisation. (Text Extrahieren)");
            AddDescription<Settings>(x => x.DocumentTransformParallelism, "Anzahl gleichzeitig verarbeiteter Dokumente beim Download. (PDF mit Textlayer erzeugen)");
        }
    }
}