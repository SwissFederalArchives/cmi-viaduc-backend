namespace CMI.Contract.Messaging
{
    public interface IRemoveArchiveRecord
    {
        long MutationId { get; set; }
        string ArchiveRecordId { get; set; }
    }
}