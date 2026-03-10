using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using System.Collections.Generic;

namespace CMI.Contract.Messaging
{
    public class GetPendingMutationsRequest
    {
    }

    public class GetPendingMutationsResponse
    {
        public List<MutationRecord> MutationRecords { get; set; }
    }

    public class UpdateMutationStatusRequest
    {
        public MutationStatusInfo Info { get; set; }
    }

    public class UpdateMutationStatusResponse
    {
        public int Result { get; set; }
    }

    public class BulkUpdateMutationStatusRequest
    {
        public List<MutationStatusInfo> Infos { get; set; }
    }

    public class BulkUpdateMutationStatusResponse
    {
        public int Result { get; set; }
    }

    public class ResetFailedSyncOperationsRequest
    {
        public int MaxRetries { get; set; }
    }

    public class ResetFailedSyncOperationsResponse
    {
        public int Result { get; set; }
    }

    public class InsertSyncActionRequest
    {
        public SyncActionDto SyncActionDto { get; set; }
    }

    public class InsertSyncActionResponse
    {
    }
    public class DeleteOldSyncActionRequest
    {
        public int DaysAgo { get; set; }
    }

    public class DeleteOldSyncActionResponse
    {
    }

}