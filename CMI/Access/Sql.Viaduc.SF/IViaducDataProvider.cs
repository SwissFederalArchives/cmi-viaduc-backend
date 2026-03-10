using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;

namespace CMI.Access.Sql.Viaduc.EF;

public interface IViaducDataProvider
{
    Task<List<MutationRecord>> GetPendingMutations();
    Task<int> UpdateMutationStatus(MutationStatusInfo info);
    Task<int> BulkUpdateMutationStatus(List<MutationStatusInfo> infos);
    Task<int> ResetFailedSyncOperations(int maxRetries);
    Task InsertSyncAction(SyncActionDto syncActionDto);
    Task DeleteOldSyncActionAsync(int daysAgo);

}