using CMI.Utilities.Common;

namespace CMI.Manager.Repository.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.FileCopyDestinationPath,
                "Lokales Zielverzeichnis des Repository Service (Ablage nach dem auslesen aus dem Repository)");
            AddDescription<Settings>(x => x.SFTPHost, " Adresse des SFTP Servers für die Übermittlung in die SSZ");
            AddDescription<Settings>(x => x.SFTPKeyFile, "Key File des SFTP Servers für die Übermittlung in die SSZ");
            AddDescription<Settings>(x => x.SFTPPassword, "Passwort des SFTP Servers für die Übermittlung in die SSZ");
            AddDescription<Settings>(x => x.SFTPPort, "Port des SFTP Servers für die Übermittlung in die SSZ");
            AddDescription<Settings>(x => x.SFTPUser, "Benutzer des SFTP Servers für die Übermittlung in die SSZ");
            AddDescription<Settings>(x => x.TempStoragePath, "Lokaler Arbeitsverzeichnis des Repository Service");
            AddDescription<Settings>(x => x.UseSFTP, "True/False ob mit dem SFTP Server übertragen werden soll.");
        }
    }
}