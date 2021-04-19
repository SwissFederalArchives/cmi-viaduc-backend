using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Contract.Repository;
using CMI.Manager.Repository.ParameterSettings;
using CMI.Manager.Repository.Properties;
using MassTransit;
using Newtonsoft.Json;
using Renci.SshNet;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace CMI.Manager.Repository
{
    public class RepositoryManager : IRepositoryManager
    {
        private const string contentFolderName = "content";
        private const string headerFolderName = "header";
        private readonly IBus bus;
        private readonly IPackageHandler handler;
        private readonly List<string> ignoredFilenameRegex;
        private readonly IPackageValidator packageValidator;
        private readonly IRepositoryDataAccess repositoryDataAccess;
        private readonly RepositorySyncSettings syncSettings;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RepositoryManager" /> class.
        /// </summary>
        /// <param name="repositoryDataAccess">The repository data access.</param>
        /// <param name="packageValidator">A validation engine to validate the package.</param>
        /// <param name="handler">A handler that creates the arelda metadata XML</param>
        /// <param name="parameterHelper">Class to read settings parameters</param>
        public RepositoryManager(IRepositoryDataAccess repositoryDataAccess, IPackageValidator packageValidator, IPackageHandler handler,
            IParameterHelper parameterHelper, IBus bus)
        {
            this.repositoryDataAccess = repositoryDataAccess;
            this.packageValidator = packageValidator;
            this.handler = handler;
            this.bus = bus;
            syncSettings = parameterHelper.GetSetting<RepositorySyncSettings>();
            ignoredFilenameRegex = syncSettings.IgnorierteDateinamenRegex.Split('\n').Select(s => s.Trim()).ToList();
        }

        /// <summary>
        ///     Gets the package.
        /// </summary>
        /// <param name="packageId">The package identifier.</param>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <returns>RepositoryPackageResult.</returns>
        public async Task<RepositoryPackageResult> GetPackage(string packageId, string archiveRecordId, int primaerdatenAuftragId)
        {
            var startTime = DateTime.Now;

            // Getting the package including the metadata.xml 
            var packageResult = await GetPackageInternal(packageId, archiveRecordId, true, new List<string>(), primaerdatenAuftragId);

            // Output duration
            var timespan = new TimeSpan(DateTime.Now.Ticks - startTime.Ticks);
            Log.Information("Package {packageId} with {SizeInBytes} bytes fetched in {TotalSeconds} seconds. Valid status is: {Valid}", packageId,
                packageResult.PackageDetails?.SizeInBytes, timespan.TotalSeconds, packageResult.Valid);

            return packageResult;
        }

        public async Task<RepositoryPackageResult> AppendPackageToArchiveRecord(ArchiveRecord archiveRecord, long mutationId, int primaerdatenId)
        {
            var startTime = DateTime.Now;

            var packageId = archiveRecord.Metadata.PrimaryDataLink;
            var archiveRecordId = archiveRecord.ArchiveRecordId;

            using (LogContext.PushProperty("packageId", packageId))
            {
                if (!string.IsNullOrEmpty(packageId) && !string.IsNullOrEmpty(archiveRecordId))
                {
                    var fileTypesToIgnore = syncSettings.IgnorierteDateitypenFuerSynchronisierung.Split(',');
                    // Getting the package, but for syncing we don't need the overhead of creating the metadata stuff
                    var packageResult = await GetPackageInternal(packageId, archiveRecordId, false, fileTypesToIgnore.Select(f => f.Trim()).ToList(),
                        primaerdatenId);

                    // Output duration
                    var timespan = new TimeSpan(DateTime.Now.Ticks - startTime.Ticks);
                    Log.Information("Package {packageId} with {SizeInBytes} bytes fetched in {TotalSeconds} seconds. Valid status is: {Valid}",
                        packageId,
                        packageResult.PackageDetails.SizeInBytes, timespan.TotalSeconds, packageResult.Valid);

                    if (packageResult.Success && packageResult.Valid)
                    {
                        // Append the package to the archive record
                        archiveRecord.PrimaryData.Add(packageResult.PackageDetails);
                        return packageResult;
                    }

                    Log.Warning(
                        "Package {packageId} for Archiverecord {archiveRecordId} not appended, because package could not be created or was invalid. ({ErrorMessage})",
                        packageId, archiveRecordId,
                        packageResult.ErrorMessage);

                    packageResult.ErrorMessage +=
                        $"{(!string.IsNullOrEmpty(packageResult.ErrorMessage) ? Environment.NewLine : string.Empty)}Package successfull status: {packageResult.Success}. Package valid status: {packageResult.Valid}";
                    return packageResult;
                }

                return new RepositoryPackageResult {ErrorMessage = "Invalid arguments for appending package"};
            }
        }

        public RepositoryPackageInfoResult ReadPackageMetadata(string packageId, string archiveRecordId)
        {
            // Init the return value
            var retVal = new RepositoryPackageInfoResult
            {
                Success = false,
                Valid = false,
                PackageDetails = new RepositoryPackage {ArchiveRecordId = archiveRecordId}
            };

            try
            {
                var allIgnoredFiles = new List<RepositoryFile>();
                var rootFolder = repositoryDataAccess.GetRepositoryRoot(packageId);
                if (rootFolder != null)
                {
                    // Get the metadata about the packages
                    retVal.PackageDetails.PackageId = packageId;
                    retVal.PackageDetails.Folders = repositoryDataAccess.GetFolders(rootFolder.Id);
                    retVal.PackageDetails.Files = repositoryDataAccess.GetFiles(rootFolder.Id, ignoredFilenameRegex, out var ignored);
                    allIgnoredFiles.AddRange(ignored);

                    // Get the sub folders of the root folders
                    foreach (var folder in retVal.PackageDetails.Folders)
                    {
                        GetFolderContent(folder, allIgnoredFiles);
                    }

                    if (allIgnoredFiles.Count > 0)
                    {
                        Log.Information("We have found {fileCount} files to ignore. These are: {files}", allIgnoredFiles.Count,
                            JsonConvert.SerializeObject(allIgnoredFiles));
                    }

                    // additional data
                    retVal.PackageDetails.SizeInBytes = GetSizeInBytesFromMetadata(retVal.PackageDetails, false);
                    retVal.PackageDetails.FileCount =
                        GetFileCountFromMetadata(retVal.PackageDetails, false); // Get all files according to DIR metadata


                    // all good
                    retVal.Success = true;
                    retVal.Valid = true;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get package metadata with id {packageId} from repository", packageId);
                retVal.ErrorMessage = "Failed to get package metadata from repository";
                throw;
            }

            return retVal;
        }


        internal async Task<RepositoryPackageResult> GetPackageInternal(string packageId, string archiveRecordId, bool createMetadataXml,
            List<string> fileTypesToIgnore, int primaerdatenAuftragId)
        {
            Debug.Assert(fileTypesToIgnore != null, "fileTypesToIgnore must not be null");

            // Init the return value
            var retVal = new RepositoryPackageResult
            {
                Success = false,
                Valid = false,
                PackageDetails = new RepositoryPackage {ArchiveRecordId = archiveRecordId}
            };
            var currentStatus = AufbereitungsStatusEnum.AuftragGestartet;

            try
            {
                var allIgnoredFiles = new List<RepositoryFile>();
                var rootFolder = repositoryDataAccess.GetRepositoryRoot(packageId);
                if (rootFolder != null)
                {
                    var tempRootFolder = GetTempRootFolder();
                    var zipFileName = GetZipFileName(tempRootFolder);

                    try
                    {
                        var watch = Stopwatch.StartNew();
                        Log.Information("Fetching the metadata for package with id {packageId}", packageId);

                        // Get the metadata about the packages
                        retVal.PackageDetails.PackageId = packageId;
                        retVal.PackageDetails.Folders = repositoryDataAccess.GetFolders(rootFolder.Id);
                        retVal.PackageDetails.Files = repositoryDataAccess.GetFiles(rootFolder.Id, ignoredFilenameRegex, out var ignored);
                        allIgnoredFiles.AddRange(ignored);

                        // Get the sub folders of the root folders
                        foreach (var folder in retVal.PackageDetails.Folders)
                        {
                            GetFolderContent(folder, allIgnoredFiles);
                        }

                        if (allIgnoredFiles.Count > 0)
                        {
                            Log.Information("We have found {fileCount} files to ignore. These are: {files}", allIgnoredFiles.Count,
                                JsonConvert.SerializeObject(allIgnoredFiles));
                        }

                        // Ensure valid file names and prevent too long paths and file names
                        packageValidator.EnsureValidPhysicalFileAndFolderNames(retVal.PackageDetails,
                            Path.Combine(tempRootFolder, contentFolderName));

                        // Now create a folder and file structure on disk matching the metadata
                        Log.Information("Creating package structure on disk for package with id {packageId}", packageId);
                        LogFreeDiskSpace(packageId);
                        CreatePackageOnDisk(tempRootFolder, retVal.PackageDetails, fileTypesToIgnore);
                        LogFreeDiskSpace(packageId);

                        // Create the metadata.xml
                        if (createMetadataXml)
                        {
                            handler.CreateMetadataXml(Path.Combine(tempRootFolder, headerFolderName), retVal.PackageDetails, allIgnoredFiles);
                        }

                        // Get some information about the package
                        var numberOfFilesInZipFile =
                            Directory.GetFiles(Path.Combine(tempRootFolder, contentFolderName), "*.*", SearchOption.AllDirectories).Length;
                        var sizeInBytes = GetSizeInBytesFromMetadata(retVal.PackageDetails, false);
                        var sizeInBytesOnDisk = Directory.GetFiles(tempRootFolder, "*.*", SearchOption.AllDirectories)
                            .Select(f => new FileInfo(f).Length).Sum();
                        var numberOfFilesInMetadata =
                            GetFileCountFromMetadata(retVal.PackageDetails, false); // Get all files according to DIR metadata
                        var numberOfFilesInMetadataRespectingIgnored =
                            GetFileCountFromMetadata(retVal.PackageDetails, true); // Get all files not counting the ignored ones
                        Log.Information("Package with id {packageId} has size {SizeInBytes:n0} bytes", packageId, sizeInBytes);

                        currentStatus = AufbereitungsStatusEnum.PrimaerdatenExtrahiert;
                        await UpdatePrimaerdatenAuftragStatus(primaerdatenAuftragId, currentStatus);

                        // Make a zip file
                        ZipFile.CreateFromDirectory(tempRootFolder, zipFileName);
                        Log.Information("ZipFile created for package with id {packageId}", packageId);
                        LogFreeDiskSpace(packageId);

                        currentStatus = AufbereitungsStatusEnum.ZipDateiErzeugt;
                        await UpdatePrimaerdatenAuftragStatus(primaerdatenAuftragId, currentStatus);

                        // Check if package is valid. 
                        // Number of files must correspond. In case of just getting the files for OCR, the createMetadataXml is false and number of files is counted differently
                        var isValidPackage = numberOfFilesInMetadata == numberOfFilesInZipFile ||
                                             !createMetadataXml && numberOfFilesInZipFile == numberOfFilesInMetadataRespectingIgnored;

                        var fi = new FileInfo(zipFileName);
                        if (isValidPackage)
                        {
                            // Copy the zip file to the final destination.
                            // Depending on the setting either by sftp or a simple file copy
                            if (Settings.Default.UseSFTP)
                            {
                                CopyBySftp(fi);
                            }
                            else
                            {
                                MoveFileToDestination(fi);
                            }
                        }

                        // Delete zip file. If it was moved it is already gone, so we check
                        if (fi.Exists)
                        {
                            fi.Delete();
                        }

                        // Construct the result
                        retVal.PackageDetails.FileCount = numberOfFilesInMetadata;
                        retVal.PackageDetails.SizeInBytes = sizeInBytes;
                        retVal.PackageDetails.PackageFileName = fi.Name;

                        // Adjust the download time with an estimated download speed
                        retVal.PackageDetails.RepositoryExtractionDuration =
                            watch.ElapsedTicks + GetProcessingTimeOfIgnoredFilesInTicks(sizeInBytes - sizeInBytesOnDisk);
                        retVal.Success = true;
                        retVal.Valid = isValidPackage;

                        currentStatus = AufbereitungsStatusEnum.PaketTransferiert;
                        await UpdatePrimaerdatenAuftragStatus(primaerdatenAuftragId, currentStatus);

                        if (!retVal.Valid)
                        {
                            var metadata = JsonConvert.SerializeObject(retVal.PackageDetails);
                            Log.Error(
                                "Have {numberOfFilesInZipFile} files in package, but should be {numberOfFilesInMetadata} files according to metadata. Metadata is {metadata}",
                                numberOfFilesInZipFile,
                                numberOfFilesInMetadata, metadata);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unknown error while creating the package with id {packageId}.", packageId);
                        LogFreeDiskSpace(packageId);

                        // Do we have a stry zip file? (out of space exception while zipping...)
                        if (File.Exists(zipFileName))
                        {
                            Log.Information("Found remains of zip file. Deleting zip file {zipFileName} for package {packageId}.", zipFileName,
                                packageId);
                            File.Delete(zipFileName);
                            Log.Information("Deleted zip file {zipFileName} for package {packageId}.", zipFileName, packageId);
                        }

                        retVal.ErrorMessage = $"Unknown error: {ex.Message}.";
                        while (ex.InnerException != null)
                        {
                            retVal.ErrorMessage += Environment.NewLine + ex.InnerException.Message;
                            ex = ex.InnerException;
                        }
                    }
                    finally
                    {
                        // Delete the temp files
                        Directory.Delete(tempRootFolder, true);
                        Log.Information("Deleted temp files for package {packageId}", packageId);
                    }
                }
                else
                {
                    Log.Warning("Could not find package with id {packageId} in the repository", packageId);
                    retVal.ErrorMessage = $"Could not find package with id {packageId} in the repository";
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to get package with id {packageId} from repository", packageId);
                LogFreeDiskSpace(packageId);
                retVal.ErrorMessage = "Failed to get package from repository";
            }

            // Bei einem Fehlerfall melden wir den letzten Status erneut, diesmal mit ErrorText an die Priorisierungsengine
            if (!retVal.Success)
            {
                await UpdatePrimaerdatenAuftragStatus(primaerdatenAuftragId, currentStatus, retVal.ErrorMessage);
            }

            return retVal;
        }


        private async Task UpdatePrimaerdatenAuftragStatus(int primaerdatenAuftragId, AufbereitungsStatusEnum status, string errorText = null)
        {
            if (primaerdatenAuftragId > 0)
            {
                Log.Information("Auftrag mit Id {PrimaerdatenAuftragId} wurde im Repository-Service auf Status {Status} gesetzt.",
                    primaerdatenAuftragId, status.ToString());

                var ep = await bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.AssetManagerUpdatePrimaerdatenAuftragStatusMessageQueue));
                await ep.Send<IUpdatePrimaerdatenAuftragStatus>(new UpdatePrimaerdatenAuftragStatus
                {
                    PrimaerdatenAuftragId = primaerdatenAuftragId,
                    Service = AufbereitungsServices.RepositoryService,
                    Status = status,
                    ErrorText = errorText
                });
            }
        }

        private static string GetZipFileName(string tempRootFolder)
        {
            var di = new DirectoryInfo(tempRootFolder);
            Debug.Assert(di.Parent != null);
            var zipFileName = Path.Combine(di.Parent.FullName, di.Name + ".zip");
            return zipFileName;
        }


        private static void MoveFileToDestination(FileInfo zipFile)
        {
            // If we have a final destination folder, move the zip file there
            var finalDestinationPath = Settings.Default.FileCopyDestinationPath;
            if (!string.IsNullOrEmpty(finalDestinationPath) && Directory.Exists(finalDestinationPath))
            {
                Log.Information("Moving zip file to the final destination.");
                // Set the new final name
                var destFileName = Path.Combine(Settings.Default.FileCopyDestinationPath, zipFile.Name);
                File.Move(zipFile.FullName, destFileName);
            }
            else
            {
                Log.Warning("Final destination path could not be found. Make sure the path {finalDestinationPath} exits and is accessible.",
                    finalDestinationPath);
            }
        }

        private static void CopyBySftp(FileInfo zipFile)
        {
            var host = Settings.Default.SFTPHost;
            var port = Settings.Default.SFTPPort;
            var user = Settings.Default.SFTPUser;
            var pwd = Settings.Default.SFTPPassword;
            var keyFile = Settings.Default.SFTPKeyFile;

            try
            {
                var connectionInfo = new ConnectionInfo(host, port,
                    user, new PasswordAuthenticationMethod(user, pwd));
                if (!string.IsNullOrEmpty(keyFile))
                {
                    connectionInfo.AuthenticationMethods.Add(new PrivateKeyAuthenticationMethod(keyFile));
                }

                using (var client = new SftpClient(connectionInfo))
                {
                    client.Connect();
                    Log.Verbose("Connected successfully to SFTP host.");
                    using (var fileStream = File.Open(zipFile.FullName, FileMode.Open, FileAccess.Read))
                    {
                        client.UploadFile(fileStream, zipFile.Name, true, progress =>
                        {
                            if (Log.IsEnabled(LogEventLevel.Verbose))
                            {
                                Log.Verbose("Upload progress for {Name}: {progress}", zipFile.Name, progress);
                            }
                        });
                    }
                }

                Log.Information("Uploaded zipFile {Name} to SFTP server.", zipFile.Name);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable upload zipFile {Name} to SFTP server. Error Message: {Message}", zipFile.Name, ex.Message);
                throw;
            }
        }

        private int GetFileCountFromMetadata(RepositoryPackage package, bool onlyExported)
        {
            var retVal = package.Files.Count(f => !onlyExported || f.Exported);
            foreach (var folder in package.Folders)
            {
                retVal += GetFileCountForFolder(folder, onlyExported);
            }

            return retVal;
        }

        private int GetFileCountForFolder(RepositoryFolder folder, bool onlyExported)
        {
            var retVal = folder.Files.Count(f => !onlyExported || f.Exported);
            foreach (var subFolder in folder.Folders)
            {
                retVal += GetFileCountForFolder(subFolder, onlyExported);
            }

            return retVal;
        }

        private long GetSizeInBytesFromMetadata(RepositoryPackage package, bool onlyExported)
        {
            var retVal = package.Files.Sum(f => onlyExported ? f.Exported ? f.SizeInBytes : 0 : f.SizeInBytes);
            foreach (var folder in package.Folders)
            {
                retVal += GetSizeInBytesForFolder(folder, onlyExported);
            }

            return retVal;
        }

        private long GetSizeInBytesForFolder(RepositoryFolder folder, bool onlyExported)
        {
            var retVal = folder.Files.Sum(f => onlyExported ? f.Exported ? f.SizeInBytes : 0 : f.SizeInBytes);
            foreach (var subFolder in folder.Folders)
            {
                retVal += GetSizeInBytesForFolder(subFolder, onlyExported);
            }

            return retVal;
        }

        private void CreatePackageOnDisk(string tempRootName, RepositoryPackage package, List<string> fileTypesToIgnore)
        {
            // Save the primary data to the content direcotry
            var contentDirectroyName = Path.Combine(tempRootName, contentFolderName);
            if (!Directory.Exists(contentDirectroyName))
            {
                Directory.CreateDirectory(contentDirectroyName);
            }

            foreach (var repositoryFolder in package.Folders)
            {
                SaveFolderContent(contentDirectroyName, repositoryFolder, fileTypesToIgnore);
            }

            // Save the files of the root folder
            SaveFiles(contentDirectroyName, package.Files, fileTypesToIgnore);
        }

        private void SaveFolderContent(string folderName, RepositoryFolder folder, List<string> fileTypesToIgnore)
        {
            var newFolderName = Path.Combine(folderName, folder.PhysicalName);
            Log.Information("Create folder with name: {newFolderName}", newFolderName);
            Directory.CreateDirectory(newFolderName);
            SaveFiles(newFolderName, folder.Files, fileTypesToIgnore);

            foreach (var subFolders in folder.Folders)
            {
                SaveFolderContent(newFolderName, subFolders, fileTypesToIgnore);
            }
        }

        private void SaveFiles(string folderName, List<RepositoryFile> files, List<string> fileTypesToIgnore)
        {
            foreach (var file in files)
            {
                try
                {
                    var newFileName = Path.Combine(folderName, file.PhysicalName);

                    // Skipping file types that need to be ignored
                    if (fileTypesToIgnore.Contains(Path.GetExtension(newFileName), StringComparer.InvariantCultureIgnoreCase))
                    {
                        Log.Information("Skipping file {newFileName}", newFileName);
                        continue;
                    }

                    Log.Information("Writing file content to disk for {newFileName}", newFileName);

                    using (var stream = repositoryDataAccess.GetFileContent(file.Id))
                    {
                        StreamToFile(stream, newFileName);
                    }

                    // Compute and convert the hash if we have a hash to compare to
                    if (!string.IsNullOrEmpty(file.Hash))
                    {
                        var hash = GetFileHash(newFileName, MapHashType(file.HashAlgorithm));
                        if (!hash.Equals(file.Hash, StringComparison.InvariantCultureIgnoreCase))
                        {
                            throw new FileHashException($"Hash for file {newFileName} was {hash} but should have been {file.Hash}");
                        }
                    }

                    file.Exported = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unable to save file to disk. Filename: {Name}", file.PhysicalName);
                    throw;
                }
            }
        }

        /// <summary>
        ///     Maps the repository value for the hash type to our enumeration
        /// </summary>
        /// <param name="hashType">Type of the hash.</param>
        /// <returns>CMI.Manager.Repository.RepositoryHashAlgorithmType.</returns>
        public RepositoryHashAlgorithmType MapHashType(string hashType)
        {
            switch (hashType.ToLowerInvariant())
            {
                case "1":
                case "md5":
                    return RepositoryHashAlgorithmType.Md5;
                case "2":
                case "sha-1":
                    return RepositoryHashAlgorithmType.Sha1;
                case "3":
                case "sha-256":
                    return RepositoryHashAlgorithmType.Sha256;
                case "4":
                case "sha-512":
                    return RepositoryHashAlgorithmType.Sha512;
                default:
                    return RepositoryHashAlgorithmType.Md5;
            }
        }

        /// <summary>
        ///     Gets the hash for a file.
        /// </summary>
        /// <param name="filename">The file.</param>
        /// <param name="hashAlgorithmType">Type of the hash algorithm.</param>
        /// <returns>System.String.</returns>
        private string GetFileHash(string filename, RepositoryHashAlgorithmType hashAlgorithmType)
        {
            byte[] hash;
            switch (hashAlgorithmType)
            {
                case RepositoryHashAlgorithmType.Md5:
                    using (var md5 = MD5.Create())
                    {
                        using (var stream = File.OpenRead(filename))
                        {
                            hash = md5.ComputeHash(stream);
                        }
                    }

                    break;
                case RepositoryHashAlgorithmType.Sha1:
                    using (var sha1 = SHA1.Create())
                    {
                        using (var stream = File.OpenRead(filename))
                        {
                            hash = sha1.ComputeHash(stream);
                        }
                    }

                    break;
                case RepositoryHashAlgorithmType.Sha256:
                    using (var sha256 = SHA256.Create())
                    {
                        using (var stream = File.OpenRead(filename))
                        {
                            hash = sha256.ComputeHash(stream);
                        }
                    }

                    break;
                case RepositoryHashAlgorithmType.Sha512:
                    using (var sha512 = SHA512.Create())
                    {
                        using (var stream = File.OpenRead(filename))
                        {
                            hash = sha512.ComputeHash(stream);
                        }
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(hashAlgorithmType), hashAlgorithmType, null);
            }

            // Convert the byte array to a string
            var sb = new StringBuilder();
            foreach (var b in hash)
            {
                sb.Append(b.ToString("x2"));
            }

            return sb.ToString();
        }

        private void StreamToFile(Stream stream, string fileName)
        {
            var fs = new FileStream(fileName, FileMode.Create, FileAccess.ReadWrite);
            var block = new byte[4096];
            while (true)
            {
                var n = stream.Read(block, 0, block.Length);
                fs.Write(block, 0, n);
                if (n == 0)
                {
                    break;
                }
            }

            fs.Close();
        }

        private static string GetTempRootFolder()
        {
            var tempRootName = Path.GetRandomFileName();
            var storagePath = Settings.Default.TempStoragePath;
            if (!Directory.Exists(storagePath))
            {
                Directory.CreateDirectory(storagePath);
            }

            var tempRootFolder = Path.Combine(storagePath, tempRootName);
            Directory.CreateDirectory(tempRootFolder);
            return tempRootFolder;
        }

        private void GetFolderContent(RepositoryFolder folder, List<RepositoryFile> ignoredFiles)
        {
            // Get the sub folders
            folder.Folders = repositoryDataAccess.GetFolders(folder.Id);

            // Get the files
            folder.Files = repositoryDataAccess.GetFiles(folder.Id, ignoredFilenameRegex, out var ignored);
            ignoredFiles.AddRange(ignored);

            // get any sub folders of the folders
            foreach (var subFolder in folder.Folders)
            {
                GetFolderContent(subFolder, ignoredFiles);
            }
        }

        private void LogFreeDiskSpace(string packageId)
        {
            try
            {
                var driveInfo = new DriveInfo(Settings.Default.TempStoragePath);

                Log.Information(
                    "Available free space on the temp storage drive is {AvailableFreeSpace:n0} bytes (Total free: {TotalFreeSpace:n0}) when processing package with id {packageId}",
                    driveInfo.AvailableFreeSpace, driveInfo.TotalFreeSpace, packageId);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Could not log free disk space.");
            }
        }

        private long GetProcessingTimeOfIgnoredFilesInTicks(long sizeInBytes)
        {
            if (sizeInBytes <= 0)
            {
                return 0;
            }

            var retVal = 0L;
            var downloadSpeed = syncSettings.RepositoryDownloadSpeedInKByte;
            var zipSpeed = syncSettings.CompressionSpeedInKByte;
            var transferSpeed = syncSettings.FileTransferSpeedInKByte;

            if (downloadSpeed > 0)
                // ReSharper disable once PossibleLossOfFraction
            {
                retVal += TimeSpan.FromSeconds(sizeInBytes / 1000 / downloadSpeed).Ticks;
            }

            if (zipSpeed > 0)
                // ReSharper disable once PossibleLossOfFraction
            {
                retVal += TimeSpan.FromSeconds(sizeInBytes / 1000 / zipSpeed).Ticks;
            }

            if (transferSpeed > 0)
                // ReSharper disable once PossibleLossOfFraction
            {
                retVal += TimeSpan.FromSeconds(sizeInBytes / 1000 / transferSpeed).Ticks;
            }

            return retVal;
        }
    }
}