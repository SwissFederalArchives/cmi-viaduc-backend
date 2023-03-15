using System;
using System.Collections.Generic;
using System.IO;
using CMI.Contract.Common;

namespace CMI.Engine.Asset.PostProcess
{
    public class PostProcessValidIiifFileTypeChecker : ProcessAnalyzerBase
    {
        public string ArchiveRecordId { get; set; }

        protected override void AnalyzeFiles(string rootOrSubFolder, List<RepositoryFile> files)
        {
            foreach (var file in files)
            {
                var sourceFile = new FileInfo(Path.Combine(rootOrSubFolder, file.PhysicalName));
                if (sourceFile.Exists || !file.Exported)
                {
                    switch (sourceFile.Extension.ToLower())
                    {
                        case ".wav":
                        case ".mpeg4":
                        case ".txt":
                        case "siard":
                            throw new ArgumentException($"File {sourceFile.Name} not suitable for viewer");
                        case ".xml":
                            if (!IsPremisFile(sourceFile))
                                throw new ArgumentException($"File {sourceFile.Name} not suitable for viewer");
                            break;
                    }
                }
            }
        }

        private bool IsPremisFile(FileInfo sourceFile)
        {
            return sourceFile.Name.EndsWith("_PREMIS.xml");
        }
    }
}

