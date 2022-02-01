using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;

namespace CMI.Manager.Asset
{
    public interface IAssetManager
    {
        /// <summary>
        ///     Extracts the fulltext and adds the resulting text to the ArchiveRecord.
        /// </summary>
        /// <returns><c>true</c> if successfull, <c>false</c> otherwise.</returns>
        Task<bool> ExtractFulltext(long mutationId, ArchiveRecord archiveRecord, int primaerdatenAuftragId);

        /// <summary>
        ///     Converts a package to a usage copy.
        /// </summary>
        /// <param name="id">Bei AssetType Gebrauchskopie ArchiveRecordId, sonst OrderItemId</param>
        /// <param name="assetType">The asset type.</param>
        /// <param name="package">The details of the package to convert</param>
        Task<PackageConversionResult> ConvertPackage(string id, AssetType assetType, bool protectWithPassword, RepositoryPackage package);

        /// <summary>
        ///     Checks the status of the package
        /// </summary>
        /// <returns><c>true</c> if [is in preparation queue] [the specified archive record identifier]; otherwise, <c>false</c>.</returns>
        Task<PreparationStatus> CheckPreparationStatus(string archiveRecordId);

        /// <summary>
        ///     Registers a preparation job in the queue.
        /// </summary>
        Task<int> RegisterJobInPreparationQueue(string archiveRecordId, string packageId, AufbereitungsArtEnum aufbereitungsArt,
            AufbereitungsServices service, List<ElasticArchiveRecordPackage> primaryData, object workload);

        /// <summary>
        ///     Removes a preparation job from the queue.
        /// </summary>
        Task UnregisterJobFromPreparationQueue(int primaerdatenAuftragId);

        /// <returns>File name of the created file</returns>
        string CreateZipFileWithPasswordFromFile(string sourceFileName, string id, AssetType assetType);

        /// <summary>
        ///     Checks for pending sync jobs in the jobs table and pushes them to the queue according the to logic
        /// </summary>
        Task ExecutePendingSyncRecords();

        /// <summary>
        ///     Checks for pending download jobs in the jobs table and pushes them to the queue according the to logic
        /// </summary>
        Task ExecutePendingDownloadRecords();

        /// <summary>
        ///     Aktualisiert den Status eines Auftrags in der PrimaerdatenAuftrag Tabelle
        /// </summary>
        /// <param name="newStatus"></param>
        /// <returns></returns>
        Task<int> UpdatePrimaerdatenAuftragStatus(IUpdatePrimaerdatenAuftragStatus newStatus);

        Task DeleteOldDownloadAndSyncRecords(int olderThanXDays);

        Task<bool> ExtractZipFile(ExtractZipArgument extractZipArgument);
    }
}