namespace CMI.Contract.Common
{
    public class HarvestStatusInfo
    {
        public int NumberOfRecordsWaitingForSync { get; set; }
        public int NumberOfRecordsCurrentlySyncing { get; set; }
        public int NumberOfRecordsWithSyncSuccess { get; set; }
        public int NumberOfRecordsWithSyncFailure { get; set; }
        public int TotalNumberOfRecordsWaitingForSync { get; set; }
        public int TotalNumberOfRecordsCurrentlySyncing { get; set; }
        public int TotalNumberOfRecordsWithSyncSuccess { get; set; }
        public int TotalNumberOfRecordsWithSyncFailure { get; set; }
    }
}