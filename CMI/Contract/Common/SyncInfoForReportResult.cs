using System.Collections.Generic;

namespace CMI.Contract.Common
{
    public class SyncInfoForReportResult
    {
        public List<SyncInfoForReport> Records { get; set; }

        public bool Success { get; set; }

        public string ErrorMessage { get; set; }

    }
}
