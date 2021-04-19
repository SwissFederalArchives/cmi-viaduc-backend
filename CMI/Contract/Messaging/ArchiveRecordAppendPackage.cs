using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class ArchiveRecordAppendPackage : IArchiveRecordAppendPackage
    {
        public long MutationId { get; set; }
        public ArchiveRecord ArchiveRecord { get; set; }
        public ElasticArchiveRecord ElasticRecord { get; set; }
        public int PrimaerdatenAuftragId { get; set; }
    }
}