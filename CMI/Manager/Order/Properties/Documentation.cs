using CMI.Utilities.Common;

namespace CMI.Manager.Order.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<ElasticConnectionSetting>(x => x.ConnectionString, "URL mit Port zum ElasticSearch");
            AddDescription<ElasticConnectionSetting>(x => x.ElasticSearchUsername, "Username für Zugriff auf Elastic Search");
            AddDescription<ElasticConnectionSetting>(x => x.ElasticSearchPWD, "Passwort für den Zugriff auf Elastic Search");
            AddDescription<DbConnectionSetting>(x => x.ConnectionString, "DB-Connectionstring zur Viaduc DB");
            AddDescription<Settings>(x => x.AssetManagerPickupPath,
                "Angabe des Pfades wo der Asset-Manager die ZIP Dateien für die Aufbereitung erwartet.");
            AddDescription<Settings>(x => x.VecteurSftpRoot, "Angabe des Root-Pfades wo Vecteur die Benutzungskopien ablegt.");
            AddDescription<Settings>(x => x.SftpLicenseKey, "Lizenzschlüssel für SFTP Komponente.");
        }
    }
}
