using CMI.Contract.Asset;
using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public interface ITransformAsset
    {
        int OrderItemId { get; set; }
        AssetType AssetType { get; set; }
        RepositoryPackage RepositoryPackage { get; set; }
        string CallerId { get; set; }
        CacheRetentionCategory RetentionCategory { get; set; }
        string Recipient { get; set; }
        string Language { get; set; }
        bool ProtectWithPassword { get; set; }
        int PrimaerdatenAuftragId { get; set; }
    }
}