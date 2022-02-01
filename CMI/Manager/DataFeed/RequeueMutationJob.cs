using System.Threading.Tasks;
using CMI.Contract.Harvest;
using CMI.Manager.DataFeed.Properties;
using Quartz;
using Serilog;

namespace CMI.Manager.DataFeed
{
    /// <summary>
    ///     Re-queues failed sync operations. That is
    ///     it will try to re-sync records that have a status of 3 (failed sync) and
    ///     that were not failed more than x times
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

            var maxRetries = Settings.Default.MaxNumberOfRetries;
            var recordsAffected = dbMutationQueueAccess.ResetFailedSyncOperations(maxRetries);

            Log.Information("Reset {recordsAffected} records to initial status.", recordsAffected);
            return Task.CompletedTask;
        }
    }
}