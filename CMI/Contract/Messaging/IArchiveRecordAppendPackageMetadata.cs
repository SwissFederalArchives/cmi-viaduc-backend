using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public interface IArchiveRecordAppendPackageMetadata
    {
        long MutationId { get; set; }
        ArchiveRecord ArchiveRecord { get; set; }
        ElasticArchiveRecord ElasticRecord { get; set; }
    }
}