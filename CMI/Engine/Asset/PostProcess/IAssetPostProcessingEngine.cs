using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;

namespace CMI.Engine.Asset.PostProcess
{
    public interface IAssetPostProcessingEngine 
    {
        Task<ProcessStepResult> ConvertJp2ToJpeg(string path, ArchiveRecord archiveRecord);

        Task<ProcessStepResult> CombineSinglePageTextExtractsToTextDocument(string path, ArchiveRecord archiveRecord);

        Task<ProcessStepResult> SaveOCRTextInSolr(string path, ArchiveRecord archiveRecord);
        
        Task<ProcessStepResult> CreateIiifManifests(string path, ArchiveRecord archiveRecord);
        Task<ProcessStepResult> DistributeIiifFiles(string path, ArchiveRecord archiveRecord);
        Task<ProcessStepResult> ContainsOnlyValidFileTypes(string path, ArchiveRecord archiveRecord);
    }
}
