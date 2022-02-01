using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CMI.Tools.CacheFilesAnalysis
{
    public class CacheFile
    {
        #region Fields and properties
        public string Name { get; private set; }

        public string DirectoryName { get; set; }

        private readonly long sizeOfZip;

        private readonly long smallestFile; 

        private readonly long biggestFile;
        
        private long AverageSizeFile => sizeOfZip / NumberOfFiles;

        public double SizeOfZipOutput => sizeOfZip / Math.Pow(1024.0, 2.0);

        public double SmallestFileOutput => smallestFile / Math.Pow(1024.0, 2.0);

        public double BiggestFileOutput => biggestFile / Math.Pow(1024.0, 2.0);

        public double AverageSizeFileOutput => AverageSizeFile / Math.Pow(1024.0, 2.0);

        public int NumberOfFiles => FilesInZip.Count;

        public List<CacheFileEntry> FilesInZip { get; }

        public int Depth { get; }

        public Dictionary<string, int> Extension { get; }

        #endregion

        public CacheFile(ZipArchive archive, FileInfo fileInfo)
        {
            Name = fileInfo.Name;
            DirectoryName = fileInfo.DirectoryName;
            FilesInZip = archive.Entries.Select(f => new CacheFileEntry(f)).ToList().Where(c => c.IsContentFile).ToList();
            if (NumberOfFiles > 0)
            {
                Depth = FilesInZip.Max(f => f.Depth);
                sizeOfZip = FilesInZip.Sum(c => c.Size);
                Extension = FilesInZip.GroupBy(g => g.Extension).ToDictionary(g => g.Key, g => g.Count());
                smallestFile = FilesInZip.Min(s => s.Size);
                biggestFile = FilesInZip.Max(s => s.Size);
            }
        }
    }
}
