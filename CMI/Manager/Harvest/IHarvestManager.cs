using CMI.Contract.Common;
using CMI.Contract.Harvest;
using System.Threading.Tasks;

namespace CMI.Manager.Harvest
{
    public interface IHarvestManager
    {
        /// <summary>
        ///     Creates an ArchiveRecord reading data from the AIS
        ///     The data structure contains all required information for
        ///     indexing and displaying the record in a web application.
        /// </summary>
        /// <param name="archiveRecordId">The id of the archive id in the AIS</param>
        /// <returns>ArchiveRecord.</returns>
        Task<ArchiveRecord> BuildArchiveRecord(string archiveRecordId);

        /// <summary>
        ///     Gets access tokens for VeId from the AIS
        /// </summary>
        /// <param name="archiveRecordId">The id of the archive id in the AIS</param>
        /// <returns>ArchiveRecordSecurity.</returns>
        Task<ArchiveRecordSecurity> GetAisAccessTokens(string archiveRecordId);

        /// <summary>
        ///     Updates the mutation status in the mutation table.
        /// </summary>
        /// <param name="info">Object with information about the change.</param>
        /// <returns>Task.</returns>
        Task<int> UpdateMutationStatus(MutationStatusInfo info);

        /// <summary>
        ///     Initiates a full resync of all archive records.
        /// </summary>
        /// <param name="info">Information about who and when the request was sent.</param>
        /// <returns>Number of added records to the mutation table</returns>
        Task InitiateFullResync(ResyncRequestInfo info);
    }
}