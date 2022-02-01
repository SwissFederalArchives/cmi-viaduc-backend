using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Contract.Common;

namespace CMI.Contract.Repository
{
    public interface IPackageHandler
    {
        Task CreateMetadataXml(string folderName, RepositoryPackage package, List<RepositoryFile> filesToIgnore);
    }
}