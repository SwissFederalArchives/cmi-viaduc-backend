using System.Threading.Tasks;
using Quartz;

namespace CMI.Manager.Asset.Jobs
{
    [DisallowConcurrentExecution]
    public class CheckPendingSyncRecordsJob : IJob
    {
        private readonly IAssetManager assetManager;

        public CheckPendingSyncRecordsJob(IAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await assetManager.ExecutePendingSyncRecords();
        }
    }
}