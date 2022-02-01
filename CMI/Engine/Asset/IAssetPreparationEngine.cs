using System.Threading.Tasks;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Engine.Asset.ParameterSettings;

namespace CMI.Engine.Asset
{
    public interface IAssetPreparationEngine
    {
        Task<PreprocessingResult> DetectAndFlagLargeDimensions(RepositoryPackage package, string tempFolder, int primaerdatenAuftragId);
        Task<PreprocessingResult> OptimizePdfIfRequired(RepositoryPackage package, string tempFolder, int primaerdatenAuftragId);
    }
}
