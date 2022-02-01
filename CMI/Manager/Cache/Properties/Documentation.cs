using CMI.Utilities.Common;

namespace CMI.Manager.Cache.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<CacheSettings>(x => x.BaseAddress, "Basis Adresse zum Cahce (Wird durch die Funktion  {MachineName} ermittelt)");
            AddDescription<CacheSettings>(x => x.BaseDirectory, "Lokales Verzeichnis des Cache");
            AddDescription<CacheSettings>(x => x.Port, "Port des Cache");
            AddDescription<CacheSettings>(x => x.SftpLicenseKey, "Lizenzschlüssel für den SFTP");
            AddDescription<CacheSettings>(x => x.SftpPrivateCertKey, "Inhalt für das SFTP Private-Zertifikat (BASE64-Encoded)");
            AddDescription<CacheSettings>(x => x.SftpPrivateCertPassword, "Passwort für das SFTP Private-Zertifikat");
        }
    }
}