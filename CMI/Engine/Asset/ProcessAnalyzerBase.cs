using System.Collections.Generic;
using System.IO;
using CMI.Contract.Common;

namespace CMI.Engine.Asset
{
    public abstract class ProcessAnalyzerBase
    {
        public virtual void AnalyzeRepositoryPackage(RepositoryPackage package, string rootFolder)
        {
            AnalyzeFiles(rootFolder, package.Files);
            AnalyzeFolders(package.Folders, rootFolder);
        }

        private void AnalyzeFolders(List<RepositoryFolder> folders, string rootOrSubFolder)
        {
            foreach (var folder in folders)
            {
                AnalyzeFiles(Path.Combine(rootOrSubFolder, folder.PhysicalName), folder.Files);
                AnalyzeFolders(folder.Folders, Path.Combine(rootOrSubFolder, folder.PhysicalName));
            }
        }

        protected abstract void AnalyzeFiles(string rootOrSubFolder, List<RepositoryFile> files);
    }
}
