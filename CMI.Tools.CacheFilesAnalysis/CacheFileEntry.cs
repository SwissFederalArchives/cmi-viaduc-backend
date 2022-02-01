using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CMI.Tools.CacheFilesAnalysis
{
    public class CacheFileEntry
    {
        public CacheFileEntry(ZipArchiveEntry entry)
        {
            IsContentFile = !string.IsNullOrEmpty(entry.Name) && entry.FullName.Contains("content/");
            if (IsContentFile)
            {
                var fi = new FileInfo(entry.Name);
                Extension = fi.Extension.ToLower();
                Size = entry.CompressedLength;
                Name = entry.Name;
                FullName = entry.FullName;
                Depth = FullName.ToCharArray().Count(n => n.Equals( '/'));
            }
        }

        public int Depth { get; set; }
        public string FullName { get; set; }
        public string Extension { get; set; }
        public long Size { get; set; }
        public string Name { get; set; }
        public bool IsContentFile { get; set; }
    }
}