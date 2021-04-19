using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CMI.Contract.Common;

namespace CMI.Manager.Repository
{
    public class PackageValidator : IPackageValidator
    {
        private const int MaxAllowedPathLength = 260;

        private readonly List<char> forbiddenFileChars = new List<char> {'?', '<', '>', ':', '|', '\\', '/', '*', '\"', '^', '`', '~'};

        private readonly List<char> forbiddenPathChars = new List<char> {'?', '<', '>', ':', '|', '\\', '/', '*', '\"', '^', '`', '~'};

        public PackageValidator()
        {
            // As per documentation: GetInvalidPathChars and GetInvalidFileNameChars DO NOT include ALL invalid chars. Therefore we are combining our personal list
            forbiddenPathChars.AddRange(Path.GetInvalidPathChars().Where(c => !forbiddenPathChars.Contains(c)));
            forbiddenFileChars.AddRange(Path.GetInvalidFileNameChars().Where(c => !forbiddenFileChars.Contains(c)));
        }

        // The official max length for files and folders is 260. But as we are creating a zip file that
        // end users will unzip in folders like C:\Temp or C:\Benutzer\MeinName\Downloads 
        // The MaxPathLength property is depending on the passed rootFolderName length.
        public int MaxPathLength { get; private set; } = 200;

        public void EnsureValidPhysicalFileAndFolderNames(RepositoryPackage package, string rootFolderName)
        {
            // Make sure, that the max path length is shortened, if the root folder name length is very long.
            // But do not make the max path length bigger than 200, as the end users unzip path might be
            MaxPathLength = MaxAllowedPathLength - rootFolderName.Length < 200 ? MaxAllowedPathLength - rootFolderName.Length : MaxPathLength;

            CreateValidNames(package);

            var packageItems = ConvertToRepositoryObject(package);
            var tryCount = 0;
            // Try as long as path is too long, and we haven't shortenend every item
            while (packageItems.Any(p => p.FullName.Length > MaxPathLength) && tryCount <= packageItems.Count)
            {
                // Get the new name for the longest element
                var itemToShorten = GetNewShorterNameForLongestElement(packageItems);

                // Update the original entry with the shortened entry
                UpdatePackage(package, itemToShorten);

                // Convert the changed package again
                packageItems = ConvertToRepositoryObject(package);

                tryCount++;
            }

            // Make sure, we do not have identical names in the same directory.
            CheckForDuplicateFilesInPackage(package);

            // Remove ending dots and spaces from names
            // Spaces shouldn't exist from cut-off or creating valid names, but just to be sure
            RemoveTrailingChars(package, ".");
            RemoveTrailingChars(package, " ");
        }

        public KeyValuePair<string, string> GetNewShorterNameForLongestElement(List<TempValidationObject> packageItems)
        {
            // Get some data for the calculations
            var maxLength = packageItems.Max(l => l.FullName.Length);
            var overflow = maxLength - MaxPathLength;
            var maxLevel = packageItems.Max(l => l.HierachyLevel);
            var averageCharsPerLevel = MaxPathLength / maxLevel;

            // Ther is nothing to do, nothing is too long
            if (overflow <= 0)
            {
                return new KeyValuePair<string, string>(null, null);
            }

            // Get the longest item
            var longestItem = packageItems.FirstOrDefault(l => l.FullName.Length == maxLength);
            if (longestItem != null)
            {
                var nameParts = longestItem.FullName.Split('\\').ToList();
                var idParts = PathCombineSpecial(longestItem.IdPath, longestItem.RepositoryId).Split('\\').ToList();

                // get the index of the longest name part
                var longestName = nameParts.First(l => l.Length == nameParts.Max(m => m.Length));
                var index = nameParts.IndexOf(longestName);
                var charsAboveAverage = longestName.Length - averageCharsPerLevel;

                // if overvlow is less that chars aboveAverage, then we just reduce by the number of overflow
                var cutOffPosition = overflow < charsAboveAverage || charsAboveAverage == 0
                    ? longestName.Length - overflow
                    : longestName.Length - charsAboveAverage;
                string newName;
                // Is it a folder, or if it is a file, then the longest part must be the last item
                if (longestItem.Type == TempValidationObjectType.Folder || index < nameParts.Count - 1)
                {
                    newName = longestName.Substring(0, cutOffPosition).Trim();
                }
                else
                {
                    var extension = longestName.LastIndexOf('.') > 0 ? longestName.Substring(longestName.LastIndexOf('.')) : string.Empty;
                    newName = longestName.Substring(0, cutOffPosition - extension.Length).Trim() + extension;
                }

                return new KeyValuePair<string, string>(idParts[index], newName);
            }

            // Should never come here
            return new KeyValuePair<string, string>(null, null);
        }

        public void UpdatePackage(RepositoryPackage package, KeyValuePair<string, string> itemToShorten)
        {
            // Neet to find the item by the id
            RepositoryFile file;

            // Try the files in the root
            if (TryFindFile(package.Files, itemToShorten.Key, out file))
            {
                file.PhysicalName = itemToShorten.Value.Trim();
                return;
            }

            // Look in the subfolders and their files
            UpdateFolderOrFile(package.Folders, itemToShorten.Key, itemToShorten.Value);
        }

        public List<TempValidationObject> ConvertToRepositoryObject(RepositoryPackage package)
        {
            var retVal = new List<TempValidationObject>();
            AddFolder(retVal, package.Folders, "", "", 0);
            AddFiles(retVal, package.Files, "", "", 0);
            return retVal;
        }

        public void CreateValidNames(RepositoryPackage package)
        {
            CreateValidFolderNames(package.Folders);
            CreateValidFileNames(package.Files);
        }

        private void CheckForDuplicateFilesInPackage(RepositoryPackage package)
        {
            CheckForDuplicateFilesInFolder(package.Folders);
            RenameDuplicateFiles(package.Files);
        }

        private void CheckForDuplicateFilesInFolder(List<RepositoryFolder> packageFolders)
        {
            foreach (var folder in packageFolders)
            {
                CheckForDuplicateFilesInFolder(folder.Folders);
                RenameDuplicateFiles(folder.Files);
            }
        }

        private void RenameDuplicateFiles(List<RepositoryFile> folderFiles)
        {
            var fileGroup = folderFiles.GroupBy(f => f.PhysicalName.ToLowerInvariant());
            foreach (var duplicateFileGroup in fileGroup.Where(fg => fg.Count() > 1))
            {
                var i = 1;
                foreach (var file in duplicateFileGroup.Skip(1))
                {
                    file.PhysicalName = $"{Path.GetFileNameWithoutExtension(file.PhysicalName)}_{i}{Path.GetExtension(file.PhysicalName)}";
                    i++;
                }
            }
        }

        private void RemoveTrailingChars(RepositoryPackage package, string nameEnding)
        {
            var packageItems = ConvertToRepositoryObject(package);
            while (packageItems.Any(p => p.Name.EndsWith(nameEnding, StringComparison.CurrentCultureIgnoreCase)))
            {
                foreach (var item in packageItems.Where(p => p.Name.EndsWith(nameEnding)))
                {
                    var newValue = item.Name;
                    // remove all trailing dots at the end
                    while (newValue.EndsWith(nameEnding))
                    {
                        newValue = newValue.Substring(0, newValue.Length - 1).Trim();
                    }

                    UpdatePackage(package, new KeyValuePair<string, string>(item.RepositoryId, newValue));
                }

                // New get new package items and test, if no forbidden endings exist.
                packageItems = ConvertToRepositoryObject(package);
            }
        }

        private bool UpdateFolderOrFile(List<RepositoryFolder> folders, string id, string newName)
        {
            // Try to find the folder in the collection
            foreach (var folder in folders)
            {
                if (folder.Id == id)
                {
                    folder.PhysicalName = newName.Trim();
                    return true;
                }

                // try the subfolders
                if (UpdateFolderOrFile(folder.Folders, id, newName))
                {
                    return true;
                }

                // No folder found, so search the files
                RepositoryFile file;
                if (TryFindFile(folder.Files, id, out file))
                {
                    file.PhysicalName = newName.Trim();
                    return true;
                }
            }

            return false;
        }

        private bool TryFindFile(List<RepositoryFile> files, string id, out RepositoryFile foundFile)
        {
            foundFile = files.FirstOrDefault(f => f.Id == id);
            return foundFile != null;
        }

        private void AddFolder(List<TempValidationObject> list, List<RepositoryFolder> folders, string parentPath, string parentIdPath,
            int parentLevel)
        {
            foreach (var folder in folders)
            {
                list.Add(new TempValidationObject
                {
                    Name = folder.PhysicalName,
                    RepositoryId = folder.Id,
                    Type = TempValidationObjectType.Folder,
                    Path = parentPath,
                    FullName = PathCombineSpecial(parentPath, folder.PhysicalName ?? ""),
                    IdPath = PathCombineSpecial(parentIdPath, folder.Id),
                    HierachyLevel = parentLevel + 1
                });
                AddFolder(list, folder.Folders, PathCombineSpecial(parentPath, folder.PhysicalName ?? ""),
                    PathCombineSpecial(parentIdPath, folder.Id), parentLevel + 1);
                AddFiles(list, folder.Files, PathCombineSpecial(parentPath, folder.PhysicalName ?? ""), PathCombineSpecial(parentIdPath, folder.Id),
                    parentLevel + 1);
            }
        }

        private void AddFiles(List<TempValidationObject> list, List<RepositoryFile> files, string parentPath, string parentIdPath, int parentLevel)
        {
            foreach (var file in files)
            {
                list.Add(new TempValidationObject
                {
                    Name = file.PhysicalName,
                    RepositoryId = file.Id,
                    Type = TempValidationObjectType.File,
                    Path = parentPath,
                    FullName = PathCombineSpecial(parentPath, file.PhysicalName ?? ""),
                    IdPath = PathCombineSpecial(parentIdPath, file.Id),
                    HierachyLevel = parentLevel + 1
                });
            }
        }

        /// <summary>
        ///     A special version for Path.Combine. Problem is, that Path.Combine throws error if illegal chars are encountered.
        ///     For our purpose here this is not relevant
        /// </summary>
        /// <param name="part1">The part1.</param>
        /// <param name="part2">The part2.</param>
        /// <returns>System.String.</returns>
        private string PathCombineSpecial(string part1, string part2)
        {
            if (part1.EndsWith("\\"))
            {
                return part1 + part2;
            }

            return part1 + "\\" + part2;
        }

        private void CreateValidFileNames(List<RepositoryFile> packageFiles)
        {
            foreach (var file in packageFiles)
            {
                file.PhysicalName = GetValidFileName(file.LogicalName).Trim();
            }
        }

        private void CreateValidFolderNames(List<RepositoryFolder> packageFolders)
        {
            foreach (var folder in packageFolders)
            {
                folder.PhysicalName = GetValidFolderName(folder.LogicalName).Trim();
                CreateValidFolderNames(folder.Folders);
                CreateValidFileNames(folder.Files);
            }
        }

        private string GetValidFolderName(string pathName)
        {
            foreach (var c in forbiddenPathChars)
            {
                pathName = pathName.Replace(c, '_');
            }

            return pathName;
        }

        private string GetValidFileName(string fileName)
        {
            foreach (var c in forbiddenFileChars)
            {
                fileName = fileName.Replace(c, '_');
            }

            return fileName;
        }
    }

    public class TempValidationObject
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string IdPath { get; set; }
        public TempValidationObjectType Type { get; set; }
        public string RepositoryId { get; set; }
        public string FullName { get; set; }
        public int HierachyLevel { get; set; }
    }

    public enum TempValidationObjectType
    {
        File,
        Folder
    }
}