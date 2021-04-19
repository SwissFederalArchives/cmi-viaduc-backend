using System.Collections.Generic;
using CMI.Contract.Common;

namespace CMI.Contract.Repository
{
    public interface IPackageHandler
    {
        void CreateMetadataXml(string folderName, RepositoryPackage package, List<RepositoryFile> filesToIgnore);
    }
}