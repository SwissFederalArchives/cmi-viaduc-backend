using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Serilog.Events;

namespace CMI.Contract.Common
{
    public static class RepositoryPackageExtension
    {
        public static List<ElasticArchiveRecordPackage> ToElasticArchiveRecordPackage(this List<RepositoryPackage> packages)
        {
            var retVal = new List<ElasticArchiveRecordPackage>();

            foreach (var repositoryPackage in packages)
            {
                var newPackage = new ElasticArchiveRecordPackage
                {
                    SizeInBytes = repositoryPackage.SizeInBytes,
                    FileCount = repositoryPackage.FileCount,
                    PackageId = repositoryPackage.PackageId,
                    RepositoryExtractionDuration = repositoryPackage.RepositoryExtractionDuration > 0
                        ? new TimeSpan?(TimeSpan.FromTicks(repositoryPackage.RepositoryExtractionDuration))
                        : null,
                    FulltextExtractionDuration = repositoryPackage.FulltextExtractionDuration > 0
                        ? new TimeSpan?(TimeSpan.FromTicks(repositoryPackage.FulltextExtractionDuration))
                        : null,
                    Items = GetRepositoryItems(repositoryPackage)
                };
                retVal.Add(newPackage);
                if (Log.IsEnabled(LogEventLevel.Verbose))
                {
                    var contentItems = newPackage.Items.Where(i => i.Type == ElasticRepositoryObjectType.File).Select(f =>
                        f.Name + ": " + (!string.IsNullOrEmpty(f.Content) ? f.Content.Substring(0, Math.Min(f.Content.Length, 255)) : ""));
                    Log.Verbose(string.Join(Environment.NewLine, contentItems));
                }
            }

            return retVal;
        }

        private static List<ElasticRepositoryObject> GetRepositoryItems(RepositoryPackage repositoryPackage)
        {
            var retVal = new List<ElasticRepositoryObject>();
            ProcessFiles(repositoryPackage.Files, "/", retVal);
            ProcessFolders(repositoryPackage.Folders, "/", retVal);
            return retVal;
        }

        private static void ProcessFolders(List<RepositoryFolder> folders, string path, List<ElasticRepositoryObject> elasticRepositoryObjects)
        {
            foreach (var folder in folders)
            {
                // Add this folder to the collection
                elasticRepositoryObjects.Add(new ElasticRepositoryObject
                {
                    Type = ElasticRepositoryObjectType.Folder,
                    RepositoryId = folder.Id,
                    Name = folder.PhysicalName,
                    LogicalName = folder.LogicalName,
                    Path = path
                });

                // Add the contained items
                var newPath = path + folder.PhysicalName + "/";
                ProcessFolders(folder.Folders, newPath, elasticRepositoryObjects);
                ProcessFiles(folder.Files, newPath, elasticRepositoryObjects);
            }
        }

        private static void ProcessFiles(List<RepositoryFile> files, string path, List<ElasticRepositoryObject> elasticRepositoryObjects)
        {
            foreach (var file in files)
            {
                elasticRepositoryObjects.Add(new ElasticRepositoryObject
                {
                    Type = ElasticRepositoryObjectType.File,
                    Content = file.ContentText,
                    Hash = file.Hash,
                    HashAlgorithm = file.HashAlgorithm,
                    MimeType = file.MimeType,
                    RepositoryId = file.Id,
                    SizeInBytes = file.SizeInBytes,
                    Name = file.PhysicalName,
                    LogicalName = file.LogicalName,
                    Path = path
                });
            }
        }
    }
}