using CMI.Utilities.Common;

namespace CMI.Manager.Viaduc.Properties
{
    public class Documentation : AbstractDocumentation
    {
        public override void LoadDescriptions()
        {
            AddDescription<DbConnectionSetting>(x => x.ConnectionString, "DB-Connectionstring zur Viaduc DB");
            AddDescription<DbConnectionSetting>(x => x.ConnectionStringEF, "DB-Connectionstring zur Viaduc DB im Entity-Framework Format");
        }
    }
}