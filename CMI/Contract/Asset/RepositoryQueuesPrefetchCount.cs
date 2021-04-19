namespace CMI.Contract.Asset
{
    public class RepositoryQueuesPrefetchCount
    {
        public ushort SyncQueuePrefetchCount { get; set; }
        public ushort DownloadQueuePrefetchCount { get; set; }
    }
}