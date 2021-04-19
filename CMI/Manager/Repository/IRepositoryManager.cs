using System.Threading.Tasks;
using CMI.Contract.Common;

namespace CMI.Manager.Repository
{
    public interface IRepositoryManager
    {
        /// <summary>
        ///     Gets the package.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <returns>RepositoryPackageResult.</returns>
        Task<RepositoryPackageResult> GetPackage(string packageId, string archiveRecordId, int primaerdatenAuftragId);

        /// <summary>
        ///     Appends the detailed package information to archive record.
        /// </summary>
        /// <param name="archiveRecord">The archive record.</param>
        /// <param name="mutationId">The mutation identifier from the sync process.</param>
        /// <returns>Details of the results</returns>
        Task<RepositoryPackageResult> AppendPackageToArchiveRecord(ArchiveRecord archiveRecord, long mutationId, int primaerdatenId);

        /// <summary>
        ///     Reads the package metadata from the repository
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="archiveRecord"></param>
        /// <returns></returns>
        RepositoryPackageInfoResult ReadPackageMetadata(string packageId, string archiveRecordId);
    }
}