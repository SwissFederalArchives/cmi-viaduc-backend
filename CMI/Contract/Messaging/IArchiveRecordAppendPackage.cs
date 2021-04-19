using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public interface IArchiveRecordAppendPackage
    {
        long MutationId { get; set; }
        ArchiveRecord ArchiveRecord { get; set; }
        ElasticArchiveRecord ElasticRecord { get; set; }
        int PrimaerdatenAuftragId { get; set; }
    }
}