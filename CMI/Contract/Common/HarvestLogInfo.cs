using System;
using System.Collections.Generic;

namespace CMI.Contract.Common
{
    public class HarvestLogInfo
    {
        public int MutationId { get; set; }
        public string ArchiveRecordId { get; set; }
        public string ArchiveRecordIdName { get; set; }
        public string ActionName { get; set; }
        public ActionStatus CurrentStatus { get; set; }
        public int NumberOfSyncRetries { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastChangeDate { get; set; }

        public List<HarvestLogInfoDetail> Details { get; set; }
    }


    public class HarvestLogInfoDetail
    {
        public int MutationActionDetailId { get; set; }
        public int MutationId { get; set; }
        public DateTime ActionDate { get; set; }
        public ActionStatus ActionStatus { get; set; }
        public string ErrorReason { get; set; }
    }
}