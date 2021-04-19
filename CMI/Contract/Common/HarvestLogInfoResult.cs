using System.Collections.Generic;

namespace CMI.Contract.Common
{
    public class HarvestLogInfoResult
    {
        public List<HarvestLogInfo> ResultSet;
        public int TotalResultSetSize { get; set; }
    }
}