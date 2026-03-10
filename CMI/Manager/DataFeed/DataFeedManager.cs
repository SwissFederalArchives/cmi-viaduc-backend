using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using CMI.Contract.Messaging;
using Serilog;
using System;
using System.Threading.Tasks;
using CMI.Manager.DataFeed.SyncLog;

namespace CMI.Manager.DataFeed
{
    public class DataFeedManager : IDataFeedManager
    {
        private readonly IDbSyncLogAccess dbSyncLogAccess;

        public DataFeedManager(IDbSyncLogAccess dbSyncLogAccess)
        {
            this.dbSyncLogAccess = dbSyncLogAccess;
        }

        public async Task HandleActaProSyncRecordAsync(ActaProSyncRecord syncRecord)
        {
            try
            {
                var now = DateTime.Now;

                var syncAction = new SyncActionDto
                {
                    ArchiveRecordId = syncRecord.ArchiveRecordId,
                    ActionType = syncRecord.Action,
                    ActionStatus = (int) ActionStatus.WaitingForSync,
                    CreatedOn = now,
                    SyncActionLogs = [new() {ActionStatusHistory = nameof(ActionStatus.WaitingForSync), LogDate = DateTime.Now}]
                };

                await dbSyncLogAccess.InsertSyncActionAsync(syncAction);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while trying to insert record from ActaPro into the SyncAction table.");
                throw;  // Causes automatic retry
            }
        }
    }
}
