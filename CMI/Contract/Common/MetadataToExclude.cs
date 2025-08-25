
namespace CMI.Contract.Common
{
    /// <summary>
    /// Indicates which metadata can be excluded when returning an ArchiveRecord from Elastic
    /// </summary>
    public enum MetadataToExclude
    {
        Nothing,
        OCRContent,
        OCRContentAndFiles
    }
}
