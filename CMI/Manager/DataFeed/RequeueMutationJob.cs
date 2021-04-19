using System.Threading.Tasks;
using CMI.Contract.Harvest;
using Quartz;
using Serilog;

namespace CMI.Manager.DataFeed
{
    /// <summary>
    ///     Re-queues failed or lost sync operations. That is
    ///     it will try to re-sync records that have a status of 3 (failed sync) or that
    ///     have a status of 1 (sync in progress) for a long time.
    /// </summary>
    /// <seealso cref="Quartz.IJob" />
    [DisallowConcurrentExecution]
    public class RequeueMutationJob : IJob
    {
        private readonly IDbMutationQueueAccess dbMutationQueueAccess;

        /// <summary>
        ///     Initializes a new instance of the <see cref="CheckMutationQueueJob" /> class.
        /// </summary>
        /// <param name="dbMutationQueueAccess">The db access class.</param>
        public RequeueMutationJob(IDbMutationQueueAccess dbMutationQueueAccess)
        {
            this.dbMutationQueueAccess = dbMutationQueueAccess;
        }

        public Task Execute(IJobExecutionContext context)
        {
            Log.Information("Starting to check if re-queuing records can be found.");

            var recordsAffected = dbMutationQueueAccess.ResetFailedOrLostSyncOperations();

            Log.Information("Reset {recordsAffected} records to initial status.", recordsAffected);
            return Task.CompletedTask;
        }
    }
}