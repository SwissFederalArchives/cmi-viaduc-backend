using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;

namespace CMI.Manager.DataFeed.SyncLog
{
    public interface IDbSyncLogAccess
    {
        /// <summary>
        ///     Gets the pending mutations from the AIS or from SyncLog table.
        /// </summary>
        /// <returns>A list with the records that need to be synced.</returns>
        Task<List<MutationRecord>> GetPendingMutations();

        /// <summary>
        ///     Makes a bulk update of the mutation status in the SyncLog table.
        /// </summary>
        /// <param name="infos">List ob objects with detailed information about the status change.</param>
        /// <returns>The number of affected records.</returns>
        Task<int> BulkUpdateMutationStatus(List<MutationStatusInfo> infos);

        /// <summary>
        ///     Reset failed sync operations in the SyncLog table to the initial status.
        /// </summary>
        /// <param name="maxRetries">Maximum number of times a failed operation is reset.</param>
        /// <returns>Number of records that were reset.</returns>
        Task<int> ResetFailedSyncOperations(int maxRetries);

        /// <summary>
        ///     Inserts a new sync action record into the SyncLog table.
        /// </summary>
        Task InsertSyncActionAsync(SyncActionDto syncAction);

        /// <summary>
        ///     Deletes all old sync action record then old then daysAgo.
        /// </summary>
        Task DeleteOldSyncActionAsync(int daysAgo);
       

    }
}