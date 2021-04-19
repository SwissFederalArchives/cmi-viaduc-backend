using System.Collections.Generic;

namespace CMI.Contract.Common
{
    /// <summary>
    ///     Class contains information about how to query the log information table.
    ///     Depending on the properties set, the sql is constructed.
    /// </summary>
    public class HarvestLogInfoRequest : DataSourceRequest
    {
        public HarvestLogInfoRequest()
        {
            ActionStatusFilterList = new List<ActionStatus>();
        }

        public QueryDateRangeEnum DateRangeFilter { get; set; }
        public string ArchiveRecordIdFilter { get; set; }
        public List<ActionStatus> ActionStatusFilterList { get; set; }
    }
}