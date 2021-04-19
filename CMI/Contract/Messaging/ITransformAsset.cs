using CMI.Contract.Asset;
using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public interface ITransformAsset
    {
        string ArchiveRecordId { get; set; }
        int OrderItemId { get; set; }
        string FileName { get; set; }
        AssetType AssetType { get; set; }
        string CallerId { get; set; }
        CacheRetentionCategory RetentionCategory { get; set; }
        string Recipient { get; set; }
        string Language { get; set; }
        bool ProtectWithPassword { get; set; }
        string PackageId { get; set; }
        int PrimaerdatenAuftragId { get; set; }
    }
}