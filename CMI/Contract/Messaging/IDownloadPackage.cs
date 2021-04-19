using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public interface IDownloadPackage
    {
        string PackageId { get; set; }
        string ArchiveRecordId { get; set; }
        string CallerId { get; set; }
        CacheRetentionCategory RetentionCategory { get; set; }
        string Recipient { get; set; }
        string DeepLinkToVe { get; set; }
        string Language { get; set; }
        int PrimaerdatenAuftragId { get; set; }
    }
}