using CMI.Contract.Common;
using CMI.Contract.Harvest;
using Serilog;
using System.Threading.Tasks;
using CMI.Manager.Harvest.SyncLog;

namespace CMI.Manager.Harvest
{
    public class HarvestManager : IHarvestManager
    {
        private readonly IDbMetadataAccess dbAccess;
        private readonly IDbSyncLogAccess syncLogAccess;
        private readonly IDbResyncAccess resyncAccess;

        public HarvestManager(IDbMetadataAccess dbAccess, IDbSyncLogAccess syncLogAccess, IDbResyncAccess resyncAccess)
        {
            this.dbAccess = dbAccess;
            this.syncLogAccess = syncLogAccess;
            this.resyncAccess = resyncAccess;
        }

        /// <summary>
        ///     Creates an ArchiveRecord reading data from the AIS
        ///     The data structure contains all required information for
        ///     indexing and displaying the record in a web application.
        /// </summary>
        /// <param name="archiveRecordId">The id of the archive id in the AIS</param>
        /// <returns></returns>
        public async Task<ArchiveRecord> BuildArchiveRecord(string archiveRecordId)
        {
            return await dbAccess.GetArchiveRecord(archiveRecordId);
        }

        /// <summary>
        ///  Gets accesstokens for a Ve_Id  from the AIS        
        /// </summary>
        /// <param name="archiveRecordId">The id of the archive id in the AIS</param>
        /// <returns></returns>
        public async Task<ArchiveRecordSecurity> GetAisAccessTokens(string archiveRecordId)
        {
            return await dbAccess.GetAisAccessTokens(archiveRecordId);
        }

        /// <summary>
        ///     Updates the mutation status in the mutation table.
        /// </summary>
        /// <param name="info">Object with information about the change.</param>
        /// <returns>Task.</returns>
        public async Task<int> UpdateMutationStatus(MutationStatusInfo info)
        {
            return await syncLogAccess.UpdateMutationStatus(info);
        }

        /// <summary>
        ///     Initiates a full resync of all archive records.
        /// </summary>
        /// <param name="info">Information about who and when the request was sent.</param>
        /// <returns>Number of added records to the mutation table</returns>
        public async Task InitiateFullResync(ResyncRequestInfo info)
        {
            Log.Information("About to initiate a full resync");
            await resyncAccess.InitiateFullResync(info);
        }
    }
}