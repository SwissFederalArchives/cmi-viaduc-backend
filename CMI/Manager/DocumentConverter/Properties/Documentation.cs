using System.Security.Cryptography.X509Certificates;
using CMI.Utilities.Common;

namespace CMI.Manager.DocumentConverter.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<DocumentConverterSettings>(x => x.BaseAddress,
                "Basis Adresse zum DocumentConverter (Wird durch die Funktion  {MachineName} ermittelt)");
            AddDescription<DocumentConverterSettings>(x => x.BaseDirectory, "Lokales Verzeichnis für den DocumentConverter");
            AddDescription<DocumentConverterSettings>(x => x.MissingAbbyyPathInstallationMessage,
                "Meldung die Ausgegeben wird wenn ABBYY nicht installiert ist");
            AddDescription<DocumentConverterSettings>(x => x.PathToAbbyyFrEngineDll, "Pfad zur lokalen DLL des ABBYY Fine Readers");
            AddDescription<DocumentConverterSettings>(x => x.Port, "Port des DocumentConverters");
            AddDescription<DocumentConverterSettings>(x => x.SftpLicenseKey, "Lizenz-Schlüssel für den Rebex SFTP-Server");
            AddDescription<DocumentConverterSettings>(x => x.OCRTextExtractionProfile, "Das Profil für die OCR Text-Extraktion bei der Synchronisation.");
            AddDescription<DocumentConverterSettings>(x => x.PDFTextLayerExtractionProfile, "Das Profil für die OCR Erkennung bei der Erstellung von Gebrauchskopien.");
            AddDescription<DocumentConverterSettings>(x => x.AbbyyEnginePoolSize, "Die Grösse des Abbyy-Engine Pools.");
            AddDescription<DocumentConverterSettings>(x => x.AbbyySerialNumber, "Die Seriennummer für Abbyy.");
        }
    }
}