using System;
using System.Collections.Generic;

namespace CMI.Contract.Common
{
    public class GetElasticLogRecordsResult
    {
        public List<ElasticLogRecord> Records { get; set; }
        public int TotalCount { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }
}