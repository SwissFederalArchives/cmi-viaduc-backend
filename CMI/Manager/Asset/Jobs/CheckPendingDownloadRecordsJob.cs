using System.Threading.Tasks;
using Quartz;

namespace CMI.Manager.Asset.Jobs
{
    [DisallowConcurrentExecution]
    public class CheckPendingDownloadRecordsJob : IJob
    {
        private readonly IAssetManager assetManager;

        public CheckPendingDownloadRecordsJob(IAssetManager assetManager)
        {
            this.assetManager = assetManager;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            await assetManager.ExecutePendingDownloadRecords();
        }
    }
}