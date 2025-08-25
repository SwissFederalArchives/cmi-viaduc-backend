using System;

namespace CMI.Tools.AutomatedCacheFilesEmpty.Models
{
    public class DownloadLogEntry
    {
        public string ReferenceCode { get; set; }
        public string Vorgang { get; set; }
        public DateTime DatumErstellungToken { get; set; } 
    }
}
