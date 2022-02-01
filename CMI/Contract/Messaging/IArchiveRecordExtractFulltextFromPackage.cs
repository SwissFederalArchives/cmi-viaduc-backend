using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public interface IArchiveRecordExtractFulltextFromPackage
    {
        long MutationId { get; set; }
        ArchiveRecord ArchiveRecord { get; set; }
        int PrimaerdatenAuftragId { get; set; }
    }

    public class ArchiveRecordExtractFulltextFromPackage : IArchiveRecordExtractFulltextFromPackage
    {
        public long MutationId { get; set; }
        public ArchiveRecord ArchiveRecord { get; set; }
        public int PrimaerdatenAuftragId { get; set; }
    }
}