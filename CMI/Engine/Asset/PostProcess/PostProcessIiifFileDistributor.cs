using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using CMI.Utilities.Common.Providers;
using Serilog;

namespace CMI.Engine.Asset.PostProcess {
    public class PostProcessIiifFileDistributor : ProcessAnalyzerBase
    {
        private readonly ViewerFileLocationSettings locationSettings;

        private readonly IStorageProvider fileProvider;
        private readonly IStorageProvider configuredProvider;
        public string RootFolder { get; set; }
        public string ArchiveRecordId { get; set; }

        public PostProcessIiifFileDistributor(ViewerFileLocationSettings locationSettings, IIndex<StorageProviders, IStorageProvider> storageProviders, StorageProviders storageProvider)
        {
            this.locationSettings = locationSettings;
            if (!storageProviders.TryGetValue(StorageProviders.File, out fileProvider))
            {
                Log.Error("No File Provider");
            }
            if (!storageProviders.TryGetValue(StorageProviders.S3, out IStorageProvider s3Provider))
            {
                Log.Error("S3 Provider not available");
            }
            configuredProvider = storageProvider == StorageProviders.File ? fileProvider : s3Provider;
        }

        protected override void AnalyzeFiles(string rootOrSubFolder, List<RepositoryFile> files)
        {
            var parts = PathHelper.ArchiveIdToPathSegments(ArchiveRecordId);
            var relPath = Path.Combine(string.Join("\\", parts.Select(p => p.ValidPath)), rootOrSubFolder.Replace(RootFolder.Equals(rootOrSubFolder, StringComparison.InvariantCultureIgnoreCase) ?
                            RootFolder : RootFolder + "\\"  , ""));
            foreach (var file in files)
            {
                var sourceFile = new FileInfo(Path.Combine(rootOrSubFolder, file.PhysicalName));
                if (sourceFile.Exists)
                {
                    var t1 = fileProvider.CopyFileAsync(sourceFile, relPath, ".txt", locationSettings.OcrOutputSaveDirectory);
                    var t2 = configuredProvider.CopyFileAsync(sourceFile, relPath, ".jpg", locationSettings.ImageOutputSaveDirectory);
                    var t3 = configuredProvider.CopyFileAsync(sourceFile, relPath, ".pdf", locationSettings.ContentOutputSaveDirectory);
                    Task.WaitAll(t1, t2, t3);
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
                var t1 = fileProvider.CopyFileAsync(file, relPath, file.Extension, locationSettings.OcrOutputSaveDirectory);
                Task.WaitAll(t1);
            }
        }
    }

}
