using CMI.Contract.Asset;
using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class PrepareForTransformationMessage: ITransformAsset
    {
        public int OrderItemId { get; set; }
        public AssetType AssetType { get; set; }
        public string CallerId { get; set; }
        public CacheRetentionCategory RetentionCategory { get; set; }
        public string Recipient { get; set; }
        public string Language { get; set; }
        public bool ProtectWithPassword { get; set; }
        public int PrimaerdatenAuftragId { get; set; }
        public RepositoryPackage RepositoryPackage { get; set; }
    }
}