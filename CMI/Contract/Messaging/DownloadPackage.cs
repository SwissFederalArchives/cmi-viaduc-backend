using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class DownloadPackage : IDownloadPackage
    {
        public string PackageId { get; set; }
        public string ArchiveRecordId { get; set; }
        public string CallerId { get; set; }
        public CacheRetentionCategory RetentionCategory { get; set; }
        public string Recipient { get; set; }
        public string DeepLinkToVe { get; set; }
        public string Language { get; set; }
        public int PrimaerdatenAuftragId { get; set; }
    }
}