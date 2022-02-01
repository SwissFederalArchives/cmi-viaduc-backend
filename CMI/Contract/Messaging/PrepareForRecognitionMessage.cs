using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class PrepareForRecognitionMessage: IArchiveRecordExtractFulltextFromPackage
    {
        public long MutationId { get; set; }
        public ArchiveRecord ArchiveRecord { get; set; }
        public int PrimaerdatenAuftragId { get; set; }
    }
}