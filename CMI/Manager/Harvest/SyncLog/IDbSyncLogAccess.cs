using System.Threading.Tasks;
using CMI.Contract.Common;

namespace CMI.Manager.Harvest.SyncLog
{
    public interface IDbSyncLogAccess
    {
        /// <summary>
        ///     Updates the mutation status of a mutation record in SyncLog table.
        /// </summary>
        /// <param name="info">Object with detailed information about the status change.</param>
        /// <returns>The number of affected records.</returns>
        Task<int> UpdateMutationStatus(MutationStatusInfo info);
    }
}