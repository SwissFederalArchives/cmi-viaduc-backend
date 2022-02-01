using System.Collections.Generic;
using System.IO;
using CMI.Contract.Common;
using CMI.Engine.Asset.ParameterSettings;

namespace CMI.Engine.Asset.PreProcess
{
    public abstract class PreProcessAnalyzer
    {
        protected readonly AssetPreparationSettings settings;
        protected readonly FileResolution fileResolution;

        protected PreProcessAnalyzer(FileResolution fileResolution, AssetPreparationSettings settings)
        {
            this.fileResolution = fileResolution;
            this.settings = settings;
        }

        public void AnalyzeRepositoryPackage(RepositoryPackage package, string tempFolder)
        {
            AnalyzeFiles(tempFolder, package.Files);
            AnalyzeFolders(package.Folders, tempFolder);
        }

        private void AnalyzeFolders(List<RepositoryFolder> folders, string tempFolder)
        {
            foreach (var folder in folders)
            {
                AnalyzeFiles(Path.Combine(tempFolder, folder.PhysicalName), folder.Files);
                AnalyzeFolders(folder.Folders, Path.Combine(tempFolder, folder.PhysicalName));
            }
        }

        protected abstract void AnalyzeFiles(string tempFolder, List<RepositoryFile> files);
    }
}
