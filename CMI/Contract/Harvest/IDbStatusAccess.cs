using CMI.Contract.Common;

namespace CMI.Contract.Harvest
{
    public interface IDbStatusAccess
    {
        /// <summary>
        ///     Gets the status information on how many records are waiting for sync, or are in sync.
        /// </summary>
        /// <param name="dateRange">A date range to analize</param>
        /// <returns>HarvestStatusInfo.</returns>
        HarvestStatusInfo GetStatusInfo(QueryDateRangeEnum dateRange);

        /// <summary>
        ///     Gets the detailed log information for the data harvesting.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>HarvestLogInfo.</returns>
        HarvestLogInfoResult GetLogInfo(HarvestLogInfoRequest request);
    }
}