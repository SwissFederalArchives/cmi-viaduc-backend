namespace CMI.Access.Sql.Viaduc
{
    public class PendingMigrationCheckResult
    {
        // Number of pending records that could be imported from local swiss-archives
        public int PendingLocal { get; set; }

        // Number of pending records that could be imported from public swiss-archives
        public int PendingPublic { get; set; }
    }
}