using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class RecognitionPostProcessingMessage : IArchiveRecordExtractFulltextFromPackage
    {
        public long MutationId { get; set; }
        public ArchiveRecord ArchiveRecord { get; set; }
        public int PrimaerdatenAuftragId { get; set; }
    }
}
