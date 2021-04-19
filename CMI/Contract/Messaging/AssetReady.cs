using CMI.Contract.Asset;
using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public class AssetReady : IAssetReady
    {
        public int OrderItemId { get; set; }
        public string ArchiveRecordId { get; set; }
        public bool Valid { get; set; }
        public string ErrorMessage { get; set; }
        public string CallerId { get; set; }
        public AssetType AssetType { get; set; }
        public CacheRetentionCategory RetentionCategory { get; set; }
        public string Recipient { get; set; }
        public int PrimaerdatenAuftragId { get; set; }
    }
}