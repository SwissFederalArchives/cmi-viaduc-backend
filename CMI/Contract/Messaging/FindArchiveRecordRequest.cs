namespace CMI.Contract.Messaging
{
    public class FindArchiveRecordRequest
    {
        public string ArchiveRecordId { get; set; }
        public bool IncludeFulltextContent { get; set; }
    }
}