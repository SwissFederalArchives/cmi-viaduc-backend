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
            AddDescription<Settings>(x => x.SftpLicenseKey, "Der Lizenzschlüssel für den Rebex Sftp Solr.");
            AddDescription<Settings>(x => x.TextExtractParallelism, "Anzahl gleichzeitig verarbeiteter Dokumente bei der Synchronisation. (Text Extrahieren)");
            AddDescription<Settings>(x => x.DocumentTransformParallelism, "Anzahl gleichzeitig verarbeiteter Dokumente beim Download. (PDF mit Textlayer erzeugen)");
            AddDescription<Settings>(x => x.SolrUrl, "Die URL wo der SOlr Server läuft.");
            AddDescription<Settings>(x => x.hOcrCopyDestinationPath, "Der Pfad wo die erkannten Texte der Bilder gespeichert werden soll (Muss im Solr Verzeichnis liegen.)");
            AddDescription<Settings>(x => x.SolrCoreName, "Der Name des erzeugten Solr Core");
            AddDescription<Settings>(x => x.IgnoreAccessTokensForManifestCheck, "Soll auch Primärdaten erzeugt werden, wenn die VE keine Ö2 Download und Fulltext Tokens besitzt. Nur zum Testen wichtig.");

            AddDescription<IiifManifest>(x => x.ApiServerUri, "URL des API Servers oder Backend Servers.");
            AddDescription<IiifManifest>(x => x.ImageServerUri, "URL des Image Servers.");
            AddDescription<IiifManifest>(x => x.PublicContentWebUri, "URL wo die Nutzdaten (JPG, PDF etc.) für den Viewer liegen.");
            AddDescription<IiifManifest>(x => x.PublicDetailRecordUri, "Basis URL um auf die Detailseite einer VE zu verlinken.");
            AddDescription<IiifManifest>(x => x.PublicOcrWebUri, "URL wo die OCR Textdateien abgelegt sind.");
            AddDescription<IiifManifest>(x => x.PublicManifestWebUri, "URL des wo die Manifest zu finden sind.");
            AddDescription<IiifManifest>(x => x.DataStorageProvider, "Welcher Provider so verwendet werden, aktuell gibt es nur dne S3 und den FileProvider.");

            AddDescription<ViewerFileLocation>(x => x.ManifestOutputSaveDirectory, "Verzeichnis wohin die Manifeste gespeichert werden");
            AddDescription<ViewerFileLocation>(x => x.ContentOutputSaveDirectory, "Verzeichnis wohin die Nutzdaten ohne Bilder abgelegt werden.");
            AddDescription<ViewerFileLocation>(x => x.OcrOutputSaveDirectory, "Verzeichnis wohin die OCR Textdaten abgelegt werden.");
            AddDescription<ViewerFileLocation>(x => x.ImageOutputSaveDirectory, "Verzeichnis wohin die Bilddateien abgelegt werden.");
            
            AddDescription<StorageProvider>(x => x.BucketName, "Der Bucket Name vom S3 Speicher.");
            AddDescription<StorageProvider>(x => x.AccessKey, "Ein Art Passwort für den S3 Speicher.");
            AddDescription<StorageProvider>(x => x.SecretAccessKey, "Ein Art Passwort für den S3 Speicher.");
            AddDescription<StorageProvider>(x => x.Region, "In welcher Region ist der S3 Speicher.");
            AddDescription<StorageProvider>(x => x.ServiceUrl, "Die Service Url vom S3 Speicher..");
        }
    }
}