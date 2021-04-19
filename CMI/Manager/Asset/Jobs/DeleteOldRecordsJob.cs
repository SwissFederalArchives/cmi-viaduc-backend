using System.Threading.Tasks;
using CMI.Manager.Asset.ParameterSettings;
using Quartz;

namespace CMI.Manager.Asset.Jobs
{
    [DisallowConcurrentExecution]
    public class DeleteOldRecordsJob : IJob
    {
        private readonly IAssetManager assetManager;
        private readonly AssetPriorisierungSettings settings;

        public DeleteOldRecordsJob(IAssetManager assetManager, AssetPriorisierungSettings settings)
        {
            this.assetManager = assetManager;
            this.settings = settings;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            var toDelete = settings.AuftraegeLoeschenAelterAlsXTage;
            await assetManager.DeleteOldDownloadAndSyncRecords(toDelete);
        }
    }
}