using CMI.Contract.Common;
using CMI.Contract.Harvest;

namespace CMI.Access.Harvest
{
    public partial class AISDataAccess : IDbStatusAccess
    {
        /// <summary>
        ///     Gets the detailed log information for the data harvesting.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>HarvestLogInfo.</returns>
        public HarvestLogInfoResult GetLogInfo(HarvestLogInfoRequest request)
        {
            return dataProvider.GetHarvestLogInfo(request);
        }

        /// <summary>
        ///     Gets the status information on how many records are waiting for sync, or are in sync.
        /// </summary>
        /// <param name="dateRange">A date range to analize</param>
        /// <returns>HarvestStatusInfo.</returns>
        public HarvestStatusInfo GetStatusInfo(QueryDateRangeEnum dateRange)
        {
            return dataProvider.GetHarvestStatusInfo(new QueryDateRange(dateRange));
        }
    }
}