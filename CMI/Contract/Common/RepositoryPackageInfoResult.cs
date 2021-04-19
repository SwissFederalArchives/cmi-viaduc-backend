namespace CMI.Contract.Common
{
    public class RepositoryPackageInfoResult
    {
        public RepositoryPackage PackageDetails { get; set; }
        public bool Success { get; set; }
        public bool Valid { get; set; }
        public string ErrorMessage { get; set; }
    }
}