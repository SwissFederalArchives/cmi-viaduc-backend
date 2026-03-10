using Quartz;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMI.Manager.DataFeed.Properties;
using CMI.Manager.DataFeed.SyncLog;

namespace CMI.Manager.DataFeed
{
    public class DeleteOldSyncActionItemsJob : IJob
    {
        private readonly IDbSyncLogAccess dbSyncLogAccess;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CheckMutationQueueJob" /> class.
        /// </summary>
        /// <param name="dbSyncLogAccess">The db access class.</param>
        public DeleteOldSyncActionItemsJob(IDbSyncLogAccess dbSyncLogAccess)
        {
            this.dbSyncLogAccess = dbSyncLogAccess;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            Log.Information("Starting to delete old SyncAction items.");

            var daysAgo = Settings.Default.DeleteSyncActionBeforeDays;
            await dbSyncLogAccess.DeleteOldSyncActionAsync(daysAgo);

        }
    }
}
