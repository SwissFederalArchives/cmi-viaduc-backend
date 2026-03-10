using System;

namespace CMI.Tools.AutomatedCacheFilesEmpty.Models
{
    public class CacheCheckResult
    {
        public string FilePath { get; set; }
        public string ArchiveRecordId { get; set; }
        public string ReferenceCode { get; set; }
        public double FileSizeInMb { get; set; } 
        public DateTime DatumErstellungToken { get; set; }
        public bool ToBeDeleted { get; set; }
        public bool HasNoViewerManifest { get; set; }
        public DateTime FileCreatedDate { get; set; }

    }
}
