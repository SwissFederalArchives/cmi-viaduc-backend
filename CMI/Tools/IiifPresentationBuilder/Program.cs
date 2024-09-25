using System;
using System.Collections.Generic;
using System.IO;
using CMI.Contract.Common;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Engine.Asset.PostProcess;
using CMI.Engine.Asset.Solr;

namespace CMI.Tools.IiifPresentationBuilder
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var root = @"C:\Temp\Final\6865820";
            var archiveRecordId = "6865820";


            var metadataFile = new FileInfo(Path.Combine(root, "header", "metadata.xml"));
            var paket = (PaketDIP) Paket.LoadFromFile(metadataFile.FullName);

            var location = new ViewerFileLocationSettings
            {
                ManifestOutputSaveDirectory = "C:\\Temp\\ManifestTest\\manifests",
                ContentOutputSaveDirectory = "",
                OcrOutputSaveDirectory = ""
            };

            // Create Manifest
            var manifestSettings = new IiifManifestSettings()
            {
                ApiServerUri = new Uri("https://viaducdev.cmiag.ch/iiif/"),
                ImageServerUri = new Uri("https://viaducdev.cmiag.ch/image/"),
                PublicManifestWebUri = new Uri("https://viaducdev.cmiag.ch/clientdev/files/manifests/"),
                PublicContentWebUri = new Uri("https://viaducdev.cmiag.ch/clientdev/files/content/"),
                PublicOcrWebUri = new Uri("https://viaducdev.cmiag.ch/clientdev/files/ocr/"),
                PublicDetailRecordUri = new Uri("https://viaducdev.cmiag.ch/clientdev/")
            };
            var manifestCreator = new PostProcessManifestCreator(manifestSettings,
                location);

            manifestCreator.IgnoreFileNotFoundExceptions = true;
            manifestCreator.CreateManifest(archiveRecordId, paket, root);

            Console.WriteLine("Erzeugt");

            Console.WriteLine("PostProcessIiifOcrIndexer");
            var iiifOcrIndexer = new PostProcessIiifOcrIndexer(new SolrConnectionInfo
                {SolrUrl = "SkipSolrForTesting", SolrHighlightingPath = root }, manifestSettings);


            var content = root + @"\content";
            var package = CreateTestData(content);
            iiifOcrIndexer.RootFolder = root;
            iiifOcrIndexer.ArchiveRecordId = archiveRecordId;
            iiifOcrIndexer.Paket = paket;
            iiifOcrIndexer.AnalyzeRepositoryPackage(package, content);

            Console.WriteLine("PostProcessIiifOcrIndexer finish");
            Console.ReadLine();

        }


        public static RepositoryPackage CreateTestData(string sourceDir)
        {
            var folderId = 0;
            var package = new RepositoryPackage
            {
                Folders = new List<RepositoryFolder>()
            };
            
            var directoryInfo = new DirectoryInfo(sourceDir );
            CreateFolder(directoryInfo, folderId, package);

            return package;
        }

        private static void CreateFolder(DirectoryInfo directoryInfo, int folderId, RepositoryPackage package)
        {
            foreach (var info in directoryInfo.GetDirectories())
            {
                folderId++;
                package.Folders.Add(
                    new()
                    {
                        Id = folderId.ToString(),
                        PhysicalName = info.Name,
                        LogicalName = directoryInfo.FullName,
                        Folders = new List<RepositoryFolder>() { }
                    });
                AddRepFiles(info, package.Folders[folderId - 1]);
                CreateFolder(info, folderId, package);
            }
        }

        private static void AddRepFiles(DirectoryInfo sourceDir, RepositoryFolder dir)
        {
            var repositoryFiles = new List<RepositoryFile>();
            var files = sourceDir.GetFiles();

            var index = 1;
            foreach (var file in files)
            {
                index++;
                repositoryFiles.Add(
                    new()
                    {
                        Id = dir.Id + "." + index,
                        PhysicalName = file.Name,
                        LogicalName = file.FullName
                    }
                );
            }

            dir.Files = repositoryFiles;
        }
    }
}
