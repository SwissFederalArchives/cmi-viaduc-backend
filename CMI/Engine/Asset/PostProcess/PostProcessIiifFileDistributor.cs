using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CMI.Contract.Common;

namespace CMI.Engine.Asset.PostProcess
{
    public class PostProcessIiifFileDistributor : ProcessAnalyzerBase
    {
        private readonly ViewerFileLocationSettings locationSettings;
        public string RootFolder { get; set; }
        public string ArchiveRecordId { get; set; }

        public PostProcessIiifFileDistributor(ViewerFileLocationSettings locationSettings)
        {
            this.locationSettings = locationSettings;
           
        }

        protected override void AnalyzeFiles(string rootOrSubFolder, List<RepositoryFile> files)
        {
            var parts = PathHelper.ArchiveIdToPathSegments(ArchiveRecordId);
            var relPath = Path.Combine( string.Join("\\", parts), rootOrSubFolder.Replace(RootFolder.Equals(rootOrSubFolder, StringComparison.InvariantCultureIgnoreCase) ? 
                RootFolder : RootFolder + "\\"  , ""));
            foreach (var file in files)
            {
                var sourceFile = new FileInfo(Path.Combine(rootOrSubFolder, file.PhysicalName));
                if (sourceFile.Exists)
                {
                    MoveFiles(sourceFile, relPath, ".txt", locationSettings.OcrOutputSaveDirectory);
                    MoveFiles(sourceFile, relPath, ".jpg", locationSettings.ImageOutputSaveDirectory);
                    MoveFiles(sourceFile, relPath, ".pdf", locationSettings.ContentOutputSaveDirectory);
                }
            }

            // Falls vorhanden verschieben
            MoveCombinedTextFiles(rootOrSubFolder, relPath);
        }

        private void MoveCombinedTextFiles(string rootOrSubFolder, string relPath)
        {
            // "_OCR.txt"
            //  "OCR-Text-komplett.zip"
            var di = new DirectoryInfo(rootOrSubFolder);
            foreach (var file in di.GetFiles().Where(f => f.Name.EndsWith("_OCR.txt", StringComparison.InvariantCultureIgnoreCase) ||
                                                          f.Name.EndsWith("OCR-Text-komplett.zip", StringComparison.InvariantCultureIgnoreCase)))
            {
                var targetFile = new FileInfo(Path.Combine(locationSettings.OcrOutputSaveDirectory, PathHelper.CreateShortValidUrlName(relPath, false), PathHelper.CreateShortValidUrlName(file.Name, true)));
                MoveFile(targetFile, file);
            }
        }

        private void MoveFiles(FileInfo sourceFile, string relPath, string extension, string targetDirectory)
        {
            var file = new FileInfo(Path.ChangeExtension(sourceFile.FullName, extension));
            if (file.Exists)
            {
                var targetFile = new FileInfo(Path.Combine(targetDirectory, PathHelper.CreateShortValidUrlName(relPath, false), PathHelper.CreateShortValidUrlName(file.Name, true)));
                MoveFile(targetFile, file);
            }
        }

        private static void MoveFile(FileInfo targetFile, FileInfo sourceFile)
        {
            // Delete existing file
            if (targetFile.Exists)
            {
                targetFile.Delete();
            }

            if (!targetFile.Directory!.Exists)
            {
                targetFile.Directory.Create();
            }

            sourceFile.MoveTo(targetFile.FullName);
        }
    }
}
