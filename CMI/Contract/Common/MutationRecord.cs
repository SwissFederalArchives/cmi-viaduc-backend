namespace CMI.Contract.Common
{
    public class MutationRecord
    {
        public long MutationId { get; set; }
        public string ArchiveRecordId { get; set; }
        public string Action { get; set; }
    }
}