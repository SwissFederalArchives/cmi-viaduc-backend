using System;
using System.Threading.Tasks;
using Aspose.Pdf;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Engine.Asset.PreProcess;
using Serilog;

namespace CMI.Engine.Asset
{
    public class AssetPreparationEngine : IAssetPreparationEngine
    {
        private readonly PreProcessAnalyzerDetectAndFlagLargeDimensions analyzerDetectAndFlagLargeDimensions;
        private readonly PreProcessAnalyzerOptimizePdf analyzerOptimizePdf;

        public AssetPreparationEngine(PreProcessAnalyzerDetectAndFlagLargeDimensions analyzerDetectAndFlagLargeDimensions,
            PreProcessAnalyzerOptimizePdf analyzerOptimizePdf)
        {
            this.analyzerDetectAndFlagLargeDimensions = analyzerDetectAndFlagLargeDimensions;
            this.analyzerOptimizePdf = analyzerOptimizePdf;
            var licensePdf = new License();
            licensePdf.SetLicense("Aspose.Total.lic");
            
        }
        public Task<PreprocessingResult> DetectAndFlagLargeDimensions(RepositoryPackage package, string tempFolder, int primaerdatenAuftragId)
        {
            try
            {
                Log.Information("Starting to detect large dimensions in documents for primaerdatenAuftrag with id {PrimaerdatenAuftragId}",
                    primaerdatenAuftragId);
                analyzerDetectAndFlagLargeDimensions.AnalyzeRepositoryPackage(package, tempFolder);
                return Task.FromResult(new PreprocessingResult { Success = true });
            }
            catch (Exception ex)
            {
                Log.Error(ex,
                    "Unexpected error while detecting large dimensions in documents for primaerdatenAuftrag with id {PrimaerdatenAuftragId}",
                    primaerdatenAuftragId);
                return Task.FromResult(new PreprocessingResult
                    { Success = false, ErrorMessage = "Unexpected error while detecting large dimensions in documents." });
            }
        }

        public Task<PreprocessingResult> OptimizePdfIfRequired(RepositoryPackage package, string tempFolder, int primaerdatenAuftragId)
        {
            try
            {
                Log.Information("Starting to detect and optimize PDF for primaerdatenAuftrag with id {PrimaerdatenAuftragId}", primaerdatenAuftragId);
                analyzerOptimizePdf.AnalyzeRepositoryPackage(package, tempFolder);
                return Task.FromResult(new PreprocessingResult() { Success = true });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error while detect and optimize PDF for primaerdatenAuftrag with id {PrimaerdatenAuftragId}",
                    primaerdatenAuftragId);
                return Task.FromResult(new PreprocessingResult()
                    { Success = false, ErrorMessage = "Unexpected error while detect and optimize PDF." });
            }
        }
    }
}
