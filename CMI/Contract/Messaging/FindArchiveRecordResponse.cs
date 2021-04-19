using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class FindArchiveRecordResponse
    {
        public string ArchiveRecordId { get; set; }
        public ElasticArchiveRecord ElasticArchiveRecord { get; set; }
    }
}