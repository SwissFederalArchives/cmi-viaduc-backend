using CMI.Utilities.Common;

namespace CMI.Access.Harvest.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<Settings>(x => x.DefaultLanguage, "Default Sprache des Connection String zur Oracle DB des AIS");
            AddDescription<Settings>(x => x.DigitalRepositoryElementIdentifier, "ElementIdentifier des Connection String zur Oracle DB des AIS");
            AddDescription<Settings>(x => x.OracleHost, "Host des Connection Strings zur Oracle DB des AIS");
            AddDescription<Settings>(x => x.OraclePassword, "Passwort des Connection Strings zur Oracle DB des AIS");
            AddDescription<Settings>(x => x.OraclePort, "Port des Connection Strings zur Oracle DB des AIS");
            AddDescription<Settings>(x => x.OracleSchemaName, "Schema des Connection Strings zur Oracle DB des AIS");
            AddDescription<Settings>(x => x.OracleSID,
                "SID des Connection Strings zur Oracle DB des AIS.  Es muss entweder die SID oder der Service Name spezifiziert sein.");
            AddDescription<Settings>(x => x.OracleServiceName,
                "Service Name des Connection Strings zur Oracle DB des AIS. Es muss entweder die SID oder der Service Name spezifiziert sein.");
            AddDescription<Settings>(x => x.OracleUser, "Benutzer des Connection Strings zur Oracle DB des AIS");
            AddDescription<Settings>(x => x.OutputSQLExecutionTimes, "SQL Execution Time Einstellung des Connection Strings zur Oracle DB des AIS");
            AddDescription<Settings>(x => x.SupportedLanguages, "Unterstützte Sprachen des Connection Strings zur Oracle DB des AIS");
            AddDescription<Settings>(x => x.ExcludedThesaurenIds, "Eine Liste mit Thesauren-Ids deren Deskriptoren von der Synchronisation ausgeschlossen werden sollen.");
        }
    }
}