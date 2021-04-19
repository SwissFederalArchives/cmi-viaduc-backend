using System.Collections.Generic;
using CMI.Contract.Common;

namespace CMI.Contract.Harvest
{
    public interface IDbMutationQueueAccess
    {
        /// <summary>
        ///     Gets the pending mutations from the AIS.
        /// </summary>
        /// <returns>A list with the records that need to be synced.</returns>
        List<MutationRecord> GetPendingMutations();

        /// <summary>
        ///     Updates the mutation status of a mutation record in the AIS.
        /// </summary>
        /// <param name="info">Object with detailed information about the status change.</param>
        /// <returns>The number of affected records.</returns>
        int UpdateMutationStatus(MutationStatusInfo info);

        /// <summary>
        ///     Makes a bulk update of the mutation status in the AIS.
        /// </summary>
        /// <param name="infos">List ob objects with detailed information about the status change.</param>
        /// <returns>The number of affected records.</returns>
        int BulkUpdateMutationStatus(List<MutationStatusInfo> infos);

        /// <summary>
        ///     Reset failed or lost synchronize operations in the mutation table to the initial status.
        /// </summary>
        /// <returns>Number of records that were reset.</returns>
        int ResetFailedOrLostSyncOperations();
    }
}