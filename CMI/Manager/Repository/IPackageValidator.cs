using CMI.Contract.Common;

namespace CMI.Manager.Repository
{
    public interface IPackageValidator
    {
        void EnsureValidPhysicalFileAndFolderNames(RepositoryPackage package, string rootFolderName);
    }
}