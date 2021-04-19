namespace CMI.Contract.Messaging
{
    public interface ISyncArchiveRecord
    {
        long MutationId { get; set; }
        string ArchiveRecordId { get; set; }
        string Action { get; set; }
    }
}