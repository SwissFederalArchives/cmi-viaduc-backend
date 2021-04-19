using CMI.Contract.Common;

namespace CMI.Contract.Messaging
{
    public interface IPackageDownloaded
    {
        RepositoryPackageResult PackageInfo { get; set; }
    }
}