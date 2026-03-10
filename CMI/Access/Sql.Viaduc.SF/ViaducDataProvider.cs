using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using Serilog;

namespace CMI.Access.Sql.Viaduc.EF
{
    public class ViaducDataProvider  : IViaducDataProvider
    {
        private readonly ViaducDb dbContext;

        public ViaducDataProvider(ViaducDb dbContext)
        {
            this.dbContext = dbContext;
        }

        public async Task<List<MutationRecord>> GetPendingMutations()
        {
            // Action 0 Waiting for Sync
            // We are taking a max of 100'000 to prevent message size overflow error in RabbitMq
            // At the current rate of getting the pending mutation once every hour this is sufficient
            // as the system cannot process more than 100'000 syncs in an hour.
            // Or if this shouldn't be enough, then we can reduce the time for the getPendingMutations job.
            var result = dbContext.SyncActions.Where(x => x.ActionStatus == 0).Take(100000);

            var pendingMutations = new List<MutationRecord>();
            foreach (var syncAction in result)
            {
                pendingMutations.Add(new MutationRecord
                {
                    Action = syncAction.ActionType,
                    ArchiveRecordId = syncAction.ArchiveRecordId,
                    MutationId = syncAction.SyncActionId
                });
            }

            return await Task.FromResult(pendingMutations);
        }

        public async Task<int> UpdateMutationStatus(MutationStatusInfo info)
        {
            try
            {
                Debug.Assert(info.MutationId > 0, "We need an existing Sync Action");

                var record = dbContext.SyncActions.FirstOrDefault(s => s.SyncActionId == info.MutationId &&
                                                                       // If the status update is only allowed from a specific existing status, 
                                                                       // add the required where clause.
                                                                       (info.ChangeFromStatus.HasValue
                    ? s.ActionStatus == (int) info.ChangeFromStatus.Value
                    : s.ActionStatus > 0));

                if (record != null)
                {
                    record.ActionStatus = (int) info.NewStatus;
                    record.NumberOfTries = record.NumberOfTries == null ? 1 : record.NumberOfTries + 1;
                    record.ModifiedOn = DateTime.Now;

                    // Add the a log entry
                    var error = string.IsNullOrEmpty(info.ErrorMessage)
                        ? null
                        : info.ErrorMessage + Environment.NewLine + Environment.NewLine + info.StackTrace;
                    var logEntry = new SyncActionLog
                    {
                        SyncActionId = info.MutationId,
                        ActionStatusHistory = info.NewStatus.ToString(),
                        LogDate = DateTime.Now,
                        ErrorReason = error
                    };
                    record.SyncActionLogs.Add(logEntry);

                    return await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                throw;
            }

            return 0;
        }

        public async Task<int> BulkUpdateMutationStatus(List<MutationStatusInfo> infos)
        {

            // Target status must be the same for all elements
            var statusGroup = infos.GroupBy(g => g.NewStatus).ToList();
            if (statusGroup.Count > 1)
            {
                throw new ArgumentException("All elements in the list must have the same NewStatus value.");
            }

            foreach (var info in infos)
            {
                if (info.MutationId <= 0)
                {
                    var newAction = new SyncAction
                    {
                        ActionStatus = (int) info.NewStatus,
                        ActionType = info.MutationType,
                        ArchiveRecordId = info.ArchiveRecordId,
                        NumberOfTries = 0,
                        CreatedOn = DateTime.Now
                    };
                  
                    var logEntry = new SyncActionLog
                    {
                        ActionStatusHistory = info.NewStatus.ToString(),
                        LogDate = DateTime.Now
                    };
                    newAction.SyncActionLogs.Add(logEntry);
                    dbContext.SyncActions.AddObject(newAction);
                }
                else
                {
                    var existingAction = dbContext.SyncActions.FirstOrDefault(s => s.SyncActionId == info.MutationId);
                    if (existingAction == null)
                    {
                        // This should not happen
                        throw new InvalidOperationException($"Didn't find existing syncAction with id {info.MutationId}");
                    }

                    existingAction.ActionStatus = (int) info.NewStatus;
                    existingAction.ModifiedOn = DateTime.Now;

                    // Add the log entry
                    var error = string.IsNullOrEmpty(info.ErrorMessage)
                        ? null
                        : info.ErrorMessage + Environment.NewLine + Environment.NewLine + info.StackTrace;
                    var logEntry = new SyncActionLog
                    {
                        SyncActionId = info.MutationId,
                        ActionStatusHistory = info.NewStatus.ToString(),
                        LogDate = DateTime.Now,
                        ErrorReason = error
                    };
                    existingAction.SyncActionLogs.Add(logEntry);
                }
            }
            
            await dbContext.SaveChangesAsync();
            return infos.Count;
        }

        public async Task<int> ResetFailedSyncOperations(int maxRetries)
        {
            var recordsToReset = dbContext.SyncActions.Where(s => s.ActionStatus == (int) ActionStatus.SyncFailed &&
                                             s.NumberOfTries < maxRetries);
           
            foreach (var syncAction in recordsToReset)
            {
                syncAction.ActionStatus = 0;
                // Add the log entry
                var logEntry = new SyncActionLog
                {
                    SyncActionId = syncAction.SyncActionId,
                    ActionStatusHistory = nameof(ActionStatus.WaitingForSync),
                    LogDate = DateTime.Now
                };
                syncAction.ModifiedOn = DateTime.Now;
                syncAction.SyncActionLogs.Add(logEntry);
            }

            return await dbContext.SaveChangesAsync();
        }

        public async Task InsertSyncAction(SyncActionDto syncActionDto)
        {
            var entity = syncActionDto.ToEntity();
            if (syncActionDto.SyncActionLogs != null && syncActionDto.SyncActionLogs.Any())
            {
                foreach (var syncActionLogDto in syncActionDto.SyncActionLogs)
                {
                    entity.SyncActionLogs.Add(syncActionLogDto.ToEntity());
                }
            }

            dbContext.SyncActions.AddObject(entity);
            await dbContext.SaveChangesAsync();
        }

        public async Task DeleteOldSyncActionAsync(int daysAgo)
        {
            var deleteDay = DateTime.Today.AddDays(-daysAgo);
            await dbContext.ExecuteStoreCommandAsync("DELETE FROM SyncAction WHERE ISNULL(ModifiedOn, CreatedOn) < @p0", deleteDay);
        }
    }
}
