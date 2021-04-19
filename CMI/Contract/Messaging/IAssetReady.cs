using CMI.Contract.Asset;
using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public interface IAssetReady
    {
        /// <summary>
        ///     Gets or sets the order item id
        /// </summary>
        int OrderItemId { get; set; }

        /// <summary>
        ///     Gets or sets the archive record identifier to which this asset belongs.
        /// </summary>
        /// <value>The archive record identifier.</value>
        string ArchiveRecordId { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this package is valid or not.
        /// </summary>
        bool Valid { get; set; }

        /// <summary>
        ///     Gets or sets the error message. The property is null or empty of the package is valid.
        /// </summary>
        string ErrorMessage { get; set; }

        /// <summary>
        ///     Gets or sets the identifier for the caller.
        /// </summary>
        string CallerId { get; set; }

        /// <summary>
        ///     Gets or sets the type of the asset.
        /// </summary>
        /// <value>The type of the asset.</value>
        AssetType AssetType { get; set; }

        CacheRetentionCategory RetentionCategory { get; set; }
        string Recipient { get; set; }
        int PrimaerdatenAuftragId { get; set; }
    }
}