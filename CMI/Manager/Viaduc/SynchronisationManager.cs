using CMI.Access.Sql.Viaduc.EF;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMI.Manager.Viaduc
{
    public class SynchronisationManager : ISynchronisationManager
    {
        private readonly ISynchronisationAccess dbSynchronisationAccess;
        private readonly IViaducDataProvider dbViaducDataProviderAccess;

        public SynchronisationManager(ISynchronisationAccess dbSynchronisationAccess, IViaducDataProvider dbViaducDataProviderAccess)
        {
            this.dbSynchronisationAccess = dbSynchronisationAccess;
            this.dbViaducDataProviderAccess = dbViaducDataProviderAccess;

        }

        public Task<List<SyncActionLogDto>> GetLogData(long id)
        {
            var result = dbSynchronisationAccess.LogData(id);
            return result;
        }

        public Task<List<SyncActionDto>> GetSyncData(int filterId)
        {
            var result = dbSynchronisationAccess.SyncData(filterId);
            return result;
        }

        public async Task<bool> BatchAddSyncActions(string[] ids, int action)
        {
            try
            {
                var infos = new List<MutationStatusInfo>();

                foreach (var id in ids)
                {
                    infos.Add(
                        new MutationStatusInfo
                        {
                            ArchiveRecordId = id,
                            NewStatus = ActionStatus.WaitingForSync,
                            MutationType = action == 1 ? "Update" : "Delete"
                        });
                }

                await dbViaducDataProviderAccess.BulkUpdateMutationStatus(infos);
                return true;
            }
            catch (System.Exception ex)
            {
                Log.Error(ex, "Unexpected error while bulk inserting records to SyncAction table.");
                return false;
            }
        }

        public Task<List<VSyncNumberPerHourDto>> GetSyncNumberPerHour(int days)
        {
            return dbSynchronisationAccess.SyncNumberPerHour(days);
        }
    }
}
