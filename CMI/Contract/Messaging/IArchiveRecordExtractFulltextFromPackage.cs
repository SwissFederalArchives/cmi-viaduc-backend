using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public interface IArchiveRecordExtractFulltextFromPackage
    {
        long MutationId { get; set; }
        ArchiveRecord ArchiveRecord { get; set; }
        int PrimaerdatenAuftragId { get; set; }
    }
}