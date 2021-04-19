using CMI.Contract.Common;
using CMI.Contract.Harvest;

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
        ArchiveRecord BuildArchiveRecord(string archiveRecordId);

        /// <summary>
        ///     Updates the mutation status in the mutation table.
        /// </summary>
        /// <param name="info">Object with information about the change.</param>
        /// <returns>Task.</returns>
        int UpdateMutationStatus(MutationStatusInfo info);

        /// <summary>
        ///     Initiates a full resync of all archive records.
        /// </summary>
        /// <param name="info">Information about who and when the request was sent.</param>
        /// <returns>Number of added records to the mutation table</returns>
        int InitiateFullResync(ResyncRequestInfo info);

        /// <summary>
        ///     Gets the status information on how many records are waiting for sync, or are in sync.
        /// </summary>
        /// <param name="dateRange">A date range to analize</param>
        /// <returns>HarvestStatusInfo.</returns>
        HarvestStatusInfo GetStatusInfo(QueryDateRangeEnum dateRange);

        /// <summary>
        ///     Gets the detailed log information for the data harvesting.
        /// </summary>
        /// <param name="request">The request object.</param>
        /// <returns>HarvestLogInfo.</returns>
        HarvestLogInfoResult GetLogInfo(HarvestLogInfoRequest request);
    }
}