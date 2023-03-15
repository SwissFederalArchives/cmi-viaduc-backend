using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;

namespace CMI.Engine.Asset.PreProcess
{
    public interface IAssetPreparationEngine
    {
        Task<ProcessStepResult> DetectAndFlagLargeDimensions(RepositoryPackage package, string tempFolder, int primaerdatenAuftragId);
        Task<ProcessStepResult> OptimizePdfIfRequired(RepositoryPackage package, string tempFolder, int primaerdatenAuftragId);
    }
}
