using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMI.Contract.Common;

namespace CMI.Manager.Asset.Tests
{
    internal static class IIIFTestDataCreator
    {
        public static RepositoryPackage CreateTestData(string sourceDir, string[] fileExtensions)
        {
            var package = new RepositoryPackage
            {
                Folders = new List<RepositoryFolder>()
                {
                    new()
                    {
                        Id = "1",
                        PhysicalName = "This is a very long path name that should be, truncated",
                        LogicalName = "This is a very long path name that should be, truncated and replaced by other means",
                        Folders = new List<RepositoryFolder>()
                        {
                            new()
                            {
                                Id = "2",
                                PhysicalName = "This is another very long path name that should be, truncated",
                                LogicalName = "This is another very long path name that should be, truncated and replaced by other means",
                            }
                        }
                    }
                }
            };

            var dir = package.Folders[0].Folders[0];
            var files = new List<RepositoryFile>();
            foreach (var fileExtension in fileExtensions)
            {
                var index = fileExtensions.ToList().IndexOf(fileExtension);
                files.Add(
                    new()
                    {
                        Id = "3" + index,
                        PhysicalName = $"Yet another very long file name that should be truncated{index}{fileExtension}",
                        LogicalName = $"Yet another very long file name that should be truncated{index}{fileExtension}",
                    }
                );
            }

            dir.Files = files;

            CreateFilesForPackage(package, sourceDir);

            return package;
        }
    

        private static void CreateFilesForPackage(RepositoryPackage repositoryPackage, string sourceDir)
        {
            CreateFolders(repositoryPackage.Folders, sourceDir);
            CreateFiles(repositoryPackage.Files, sourceDir);

        }

        private static void CreateFiles(List<RepositoryFile> files, string folderName)
        {
            foreach (var file in files)
            {
                var fullFileName = Path.Combine(folderName, file.PhysicalName);
                if (!File.Exists(fullFileName))
                {
                    File.WriteAllText(fullFileName, "Test");
                }
            }
        }

        private static void CreateFolders(List<RepositoryFolder> folders, string folderName)
        {
            foreach (var folder in folders)
            {
                var fullFolderName = Path.Combine(folderName, folder.PhysicalName);
                if (!Directory.Exists(fullFolderName))
                {
                    Directory.CreateDirectory(fullFolderName);
                }
                CreateFiles(folder.Files, fullFolderName);
                CreateFolders(folder.Folders, fullFolderName);
            }
        }
    }
}
