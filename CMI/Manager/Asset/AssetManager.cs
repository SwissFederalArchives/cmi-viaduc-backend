using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.Asset;
using CMI.Engine.Security;
using CMI.Manager.Asset.Consumers;
using CMI.Manager.Asset.ParameterSettings;
using CMI.Manager.Asset.Properties;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using MassTransit;
using Newtonsoft.Json;
using Serilog;
using ZipFile = System.IO.Compression.ZipFile;

namespace CMI.Manager.Asset
{
    public class AssetManager : IAssetManager, IOcrTester
    {
        private readonly SchaetzungAufbereitungszeitSettings aufbereitungsZeitSettings;
        private readonly IPrimaerdatenAuftragAccess auftragAccess;
        private readonly IBus bus;
        private readonly IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse> indexClient;
        private readonly AssetPackageSizeDefinition packageSizeDefinition;
        private readonly IParameterHelper parameterHelper;
        private readonly PasswordHelper passwordHelper;
        private readonly IPreparationTimeCalculator preparationCalculator;
        private readonly IPackagePriorizationEngine priorizationEngine;
        private readonly IRenderEngine renderEngine;
        private readonly IScanProcessor scanProcessor;
        private readonly ITextEngine textEngine;
        private readonly ITransformEngine transformEngine;


        public AssetManager(ITextEngine textEngine, IRenderEngine renderEngine, ITransformEngine transformEngine, PasswordHelper passwordHelper,
            IParameterHelper parameterHelper,
            IScanProcessor scanProcessor, IPreparationTimeCalculator preparationCalculator, IPrimaerdatenAuftragAccess auftragAccess,
            IRequestClient<FindArchiveRecordRequest, FindArchiveRecordResponse> indexClient,
            IPackagePriorizationEngine priorizationEngine, IBus bus)
        {
            this.textEngine = textEngine;
            this.renderEngine = renderEngine;
            this.transformEngine = transformEngine;
            this.passwordHelper = passwordHelper;
            this.parameterHelper = parameterHelper;
            this.scanProcessor = scanProcessor;
            this.preparationCalculator = preparationCalculator;
            this.auftragAccess = auftragAccess;
            this.indexClient = indexClient;
            this.priorizationEngine = priorizationEngine;
            this.bus = bus;
            aufbereitungsZeitSettings = parameterHelper.GetSetting<SchaetzungAufbereitungszeitSettings>();
            // read and convert priorisierungs settings
            var settings = parameterHelper.GetSetting<AssetPriorisierungSettings>();
            packageSizeDefinition = JsonConvert.DeserializeObject<AssetPackageSizeDefinition>(settings.PackageSizes);
        }


        /// <summary>
        ///     Extracts the fulltext and adds the resulting text to the ArchiveRecord.
        /// </summary>
        /// <param name="mutationId">The mutation identifier.</param>
        /// <param name="archiveRecord">The archive record.</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> ExtractFulltext(long mutationId, ArchiveRecord archiveRecord, int primaerdatenAuftragStatusId)
        {
            var packages = archiveRecord.PrimaryData;
            var processingTimeForMissingFiles = 0L;

            foreach (var repositoryPackage in packages.Where(p => !string.IsNullOrEmpty(p.PackageFileName)))
            {
                var packageFileName = Path.Combine(Settings.Default.PickupPath, repositoryPackage.PackageFileName);
                var fi = new FileInfo(packageFileName);
                var watch = Stopwatch.StartNew();

                if (File.Exists(fi.FullName))
                {
                    Log.Information("Found zip file {Name}. Starting to extract...", fi.Name);
                    var tempFolder = Path.Combine(fi.DirectoryName, fi.Name.Remove(fi.Name.Length - fi.Extension.Length));
                    try
                    {
                        ZipFile.ExtractToDirectory(packageFileName, tempFolder);
                        var sizeInBytesOnDisk = Directory.GetFiles(tempFolder, "*.*", SearchOption.AllDirectories).Select(f => new FileInfo(f).Length)
                            .Sum();

                        var status = new UpdatePrimaerdatenAuftragStatus
                        {
                            PrimaerdatenAuftragId = primaerdatenAuftragStatusId,
                            Service = AufbereitungsServices.AssetService,
                            Status = AufbereitungsStatusEnum.ZipEntpackt
                        };
                        await UpdatePrimaerdatenAuftragStatus(status);

                        await ProcessFiles(repositoryPackage.Files, Path.Combine(tempFolder, "content"), archiveRecord.ArchiveRecordId);
                        await ProcessFolders(repositoryPackage.Folders, Path.Combine(tempFolder, "content"), archiveRecord.ArchiveRecordId);

                        // if we are here everything is okay
                        Log.Information("Successfully processed (fulltext extracted) zip file {Name}", fi.Name);
                        processingTimeForMissingFiles += GetProcessingTimeOfIgnoredFilesInTicks(repositoryPackage.SizeInBytes - sizeInBytesOnDisk);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Unexpected error while extracting full text. Error Message is: {Message}", ex.Message);
                        return false;
                    }
                    finally
                    {
                        // Delete the temp files
                        Directory.Delete(tempFolder, true);
                        File.Delete(packageFileName);
                    }
                }
                else
                {
                    Log.Warning("Unable to find the zip file {packageFileName}. No text was extracted.", packageFileName);
                    return false;
                }

                repositoryPackage.FulltextExtractionDuration = watch.ElapsedTicks + processingTimeForMissingFiles;
            }

            return true;
        }

        /// <summary>
        ///     Converts a package to a usage copy.
        /// </summary>
        /// <param name="id">ArchiveRecordId oder OrderItemId</param>
        /// <param name="assetType">The asset type.</param>
        /// <param name="fileName">Name of the package file to convert.</param>
        /// <param name="packageId">The id of the ordered package</param>
        /// <returns>PackageConversionResult.</returns>
        public async Task<PackageConversionResult> ConvertPackage(string id, AssetType assetType, bool protectWithPassword, string fileName,
            string packageId)
        {
            var retVal = new PackageConversionResult {Valid = true};
            var packageFileName = Path.Combine(Settings.Default.PickupPath, fileName);
            var fi = new FileInfo(packageFileName);

            // Make sure Gebrauchskopien have a packageId
            if (assetType == AssetType.Gebrauchskopie && string.IsNullOrEmpty(packageId))
            {
                throw new InvalidOperationException("Assets of type <Gebrauchskopie> require a packageId");
            }

            if (File.Exists(fi.FullName))
            {
                Log.Information("Found zip file {Name}. Starting to extract...", fi.Name);
                var tempFolder = Path.Combine(fi.DirectoryName, fi.Name.Remove(fi.Name.Length - fi.Extension.Length));
                try
                {
                    // Extract zip file to disk
                    ZipFile.ExtractToDirectory(packageFileName, tempFolder);

                    if (assetType == AssetType.Benutzungskopie)
                    {
                        ConvertAreldaMetadataXml(tempFolder);
                    }

                    var metadataFile = Path.Combine(tempFolder, "header", "metadata.xml");
                    var paket = (PaketDIP) Paket.LoadFromFile(metadataFile);

                    // Create pdf documents from scanned jpeg 2000 scans.
                    scanProcessor.ConvertSingleJpeg2000ScansToPdfDocuments(paket, tempFolder,
                        parameterHelper.GetSetting<ScansZusammenfassenSettings>());

                    // Get all the files from the subdirectory "content" in the extracted directory
                    var files = new DirectoryInfo(Path.Combine(tempFolder, "content")).GetFiles("*.*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        Log.Information("Start extracting text for file: {file} for archive record or order id {id}", file, id);
                        var convertedFile = await ConvertFile(file, paket, tempFolder);
                        // Delete the original file, if the convertedFile exists and is not the same as the original file.
                        // In case of PDF the name of the original and converted file could be the same. --> PDF to PDF with OCR
                        if (!string.IsNullOrEmpty(convertedFile) && File.Exists(convertedFile) && convertedFile != file.FullName)
                        {
                            file.Delete();
                        }
                    }

                    paket.Generierungsdatum = DateTime.Today;
                    ((Paket) paket).SaveToFile(metadataFile);

                    AddReadmeFile(tempFolder);
                    AddDesignFiles(tempFolder);
                    CreateIndexHtml(tempFolder, packageId);

                    // Create zip file with the name of the archive
                    var finalZipFolder = Path.Combine(fi.DirectoryName, assetType.ToString(), id);
                    var finalZipFile = finalZipFolder + ".zip";
                    CreateZipFile(finalZipFolder, finalZipFile, tempFolder, protectWithPassword, id);

                    retVal.FileName = finalZipFile;

                    // if we are here everything is groovy
                    Log.Information("Successfully processed (converted formats) zip file {Name}", fi.Name);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected exception while converting the package.");
                    retVal.Valid = false;
                    retVal.ErrorMessage = $"Unexpected exception while converting the package.\nException:\n{ex}";
                    return retVal;
                }
                finally
                {
                    // Delete the temp files
                    if (Directory.Exists(tempFolder))
                    {
                        Directory.Delete(tempFolder, true);
                    }
                }
            }
            else
            {
                Log.Warning("Unable to find the zip file {packageFileName} for conversion.", packageFileName);
                retVal.Valid = false;
                retVal.ErrorMessage = $"Unable to find the zip file {packageFileName} for conversion.";
                return retVal;
            }

            return retVal;
        }


        /// <summary>
        ///     Determines whether the asset is in the preperation queue.
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        /// <returns><c>true</c> if [is in preparation queue] [the specified archive record identifier]; otherwise, <c>false</c>.</returns>
        public async Task<PreparationStatus> CheckPreparationStatus(string archiveRecordId)
        {
            var auftrag = await auftragAccess.GetLaufendenAuftrag(int.Parse(archiveRecordId), AufbereitungsArtEnum.Download);
            if (auftrag != null)
            {
                return new PreparationStatus
                {
                    PackageIsInPreparationQueue = true,
                    AddedToQueueOn = auftrag.CreatedOn,
                    EstimatedPreparationDuration = auftrag.GeschaetzteAufbereitungszeit != null
                        ? TimeSpan.FromSeconds(auftrag.GeschaetzteAufbereitungszeit.Value)
                        : TimeSpan.Zero
                };
            }

            Log.Information("Asset for VE {VEID} is NOT in preparationQueue.", archiveRecordId);
            var archiveRecord = await indexClient.Request(new FindArchiveRecordRequest {ArchiveRecordId = archiveRecordId});
            var retValue = new PreparationStatus
            {
                AddedToQueueOn = DateTime.MinValue,
                EstimatedPreparationDuration = preparationCalculator.EstimatePreparationDuration(archiveRecord.ElasticArchiveRecord.PrimaryData,
                    aufbereitungsZeitSettings.KonvertierungsgeschwindigkeitAudio,
                    aufbereitungsZeitSettings.KonvertierungsgeschwindigkeitVideo)
            };
            return retValue;
        }

        /// <summary>
        ///     Registers a preparation job in the queue.
        /// </summary>
        /// <param name="archiveRecordId">The archive record identifier.</param>
        public async Task<int> RegisterJobInPreparationQueue(string archiveRecordId, string packageId, AufbereitungsArtEnum aufbereitungsArt,
            AufbereitungsServices service, List<ElasticArchiveRecordPackage> primaryData, object workload)
        {
            var preperationTime = preparationCalculator.EstimatePreparationDuration(primaryData,
                aufbereitungsZeitSettings.KonvertierungsgeschwindigkeitAudio,
                aufbereitungsZeitSettings.KonvertierungsgeschwindigkeitVideo).TotalSeconds;

            var auftrag = new PrimaerdatenAuftrag
            {
                VeId = int.Parse(archiveRecordId),
                AufbereitungsArt = aufbereitungsArt,
                PackageId = packageId,
                Service = service,
                Status = AufbereitungsStatusEnum.Registriert,
                GroesseInBytes = primaryData.Sum(p => p.SizeInBytes),
                GeschaetzteAufbereitungszeit = Convert.ToInt32(preperationTime),
                Workload = JsonConvert.SerializeObject(workload),
                PackageMetadata = JsonConvert.SerializeObject(primaryData),
                PriorisierungsKategorie = GetPriorisierungskategorie(aufbereitungsArt, primaryData.Sum(p => p.SizeInBytes), workload)
            };

            var auftragId = await auftragAccess.CreateOrUpdateAuftrag(auftrag);

            Log.Information("{METHOD} for VE {VEID} with {STATUS}. AuftragId is {auftragId}",
                nameof(RegisterJobInPreparationQueue),
                archiveRecordId,
                auftragId > 0 ? "SUCCEEDED" : "FAILED",
                auftragId);

            return auftragId;
        }

        /// <summary>
        ///     Removes a preparation job from the queue.
        /// </summary>
        /// <param name="primaerdatenAuftragId">Theprimary identifier for a job.</param>
        public async Task UnregisterJobFromPreparationQueue(int primaerdatenAuftragId)
        {
            var auftrag = await auftragAccess.GetPrimaerdatenAuftrag(primaerdatenAuftragId);
            if (auftrag != null)
            {
                // To make sure, that we don't have a race condition at the end of an Auftrag
                // We wait here a bit. In production the status changes for AuftragErledigt and ImCacheAbgelegt
                // were sometimes only a few miliseconds apart. Sometimes the AuftragErledigt before the ImCacheAbgelegt.
                await Task.Delay(5000);

                // Indem wir den Status auf erledigt stellen, ist der Auftrag abgearbeitet
                var logId = await auftragAccess.UpdateStatus(new PrimaerdatenAuftragLog
                {
                    PrimaerdatenAuftragId = auftrag.PrimaerdatenAuftragId,
                    Status = AufbereitungsStatusEnum.AuftragErledigt,
                    Service = AufbereitungsServices.AssetService
                });

                Log.Information("{METHOD} for VE {VEID} {STATUS}. AuftragId is {PrimaerdatenAuftragId}",
                    nameof(UnregisterJobFromPreparationQueue),
                    auftrag.VeId,
                    logId > 0 ? "SUCCEEDED" : "FAILED",
                    auftrag.PrimaerdatenAuftragId);
            }
            else
            {
                Log.Information("Did not find running job in table. Should have found a record for PrimaerdatenAuftrag with id of {archiveId}",
                    primaerdatenAuftragId);
            }
        }

        /// <returns>File name of the created file</returns>
        public string CreateZipFileWithPasswordFromFile(string sourceFileName, string id, AssetType assetType)
        {
            var tempFolder = Path.Combine(Settings.Default.PickupPath, Guid.NewGuid().ToString());
            var moveSource = Path.Combine(Settings.Default.PickupPath, sourceFileName);
            var moveDest = Path.Combine(tempFolder, sourceFileName);

            Directory.CreateDirectory(tempFolder);
            File.Move(moveSource, moveDest);

            var destinationArchiveFolder = Path.Combine(Settings.Default.PickupPath, assetType.ToString());
            var destinationArchiveFileName = Path.Combine(destinationArchiveFolder, id + ".zip");

            Directory.CreateDirectory(destinationArchiveFolder);
            CreateZipFileWithPasswordFromDirectory(tempFolder, destinationArchiveFileName, passwordHelper.GetHashPassword(id));
            Directory.Delete(tempFolder, true);

            return destinationArchiveFileName;
        }

        public async Task ExecutePendingSyncRecords()
        {
            var newJobs = await priorizationEngine.GetNextJobsForExecution(AufbereitungsArtEnum.Sync);
            foreach (var channelJob in newJobs)
            foreach (var auftragId in channelJob.Value)
            {
                var auftrag = await auftragAccess.GetPrimaerdatenAuftrag(auftragId);
                var syncPackage = JsonConvert.DeserializeObject<ArchiveRecordAppendPackage>(auftrag.Workload);
                syncPackage.PrimaerdatenAuftragId = auftrag.PrimaerdatenAuftragId;

                try
                {
                    var logId = await UpdatePrimaerdatenAuftragStatus(new UpdatePrimaerdatenAuftragStatus
                    {
                        PrimaerdatenAuftragId = auftrag.PrimaerdatenAuftragId,
                        Status = AufbereitungsStatusEnum.AuftragGestartet,
                        Service = AufbereitungsServices.AssetService,
                        Verarbeitungskanal = channelJob.Key
                    });

                    var ep = await bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.RepositoryManagerArchiveRecordAppendPackageMessageQueue));
                    await ep.Send<IArchiveRecordAppendPackage>(syncPackage);
                    Log.Information("Put {CommandName} message on repository queue with mutation ID: {MutationId}",
                        nameof(IArchiveRecordAppendPackage), syncPackage.MutationId);

                    Log.Information(
                        "Auftrag mit Id {PrimaerdatenAuftragId} wurde gestartet und an den Repository Service übergeben. LogId ist: {logId}",
                        auftrag.PrimaerdatenAuftragId, logId);
                }
                catch (Exception ex)
                {
                    // Tritt ein Fehler auf, wenn die Message auf die Queue gelegt werden soll. Sind die Aufträge registriert und blockieren die Auftragstabelle.
                    // Da keine weitere Verarbeitung mehr stattfindet, bleibt die ganze Verarbeitung stehen. Im Fehlerfall setzen wird den Status zurück.
                    Log.Error(ex,
                        "Unexpected error while pushing new jobs into the PrimaerdatenAuftrag table or pusing the items on the queue. Resetting PrimaerdatenAuftragStatus");
                    await UpdatePrimaerdatenAuftragStatus(new UpdatePrimaerdatenAuftragStatus
                    {
                        PrimaerdatenAuftragId = auftrag.PrimaerdatenAuftragId,
                        Status = AufbereitungsStatusEnum.Registriert,
                        Service = AufbereitungsServices.AssetService,
                        Verarbeitungskanal = null
                    });
                    Log.Information("Reset PrimaerdatenAuftragStatus to {status} for auftrag with id {primaerdatenAuftragId}",
                        AufbereitungsStatusEnum.Registriert, auftrag.PrimaerdatenAuftragId);
                }
            }
        }

        public async Task ExecutePendingDownloadRecords()
        {
            var newJobs = await priorizationEngine.GetNextJobsForExecution(AufbereitungsArtEnum.Download);
            foreach (var channelJob in newJobs)
            foreach (var auftragId in channelJob.Value)
            {
                var auftrag = await auftragAccess.GetPrimaerdatenAuftrag(auftragId);
                var downloadPackage = JsonConvert.DeserializeObject<DownloadPackage>(auftrag.Workload);
                downloadPackage.PrimaerdatenAuftragId = auftrag.PrimaerdatenAuftragId;

                var logId = await UpdatePrimaerdatenAuftragStatus(new UpdatePrimaerdatenAuftragStatus
                {
                    PrimaerdatenAuftragId = auftrag.PrimaerdatenAuftragId,
                    Status = AufbereitungsStatusEnum.AuftragGestartet,
                    Service = AufbereitungsServices.AssetService,
                    Verarbeitungskanal = channelJob.Key
                });
                Log.Information("Auftrag mit Id {PrimaerdatenAuftragId} wurde gestartet und an den Repository Service übergeben. LogId ist: {logId}",
                    auftrag.PrimaerdatenAuftragId, logId);

                var ep = await bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.RepositoryManagerDownloadPackageMessageQueue));
                await ep.Send<IDownloadPackage>(downloadPackage);
                Log.Information("Put {CommandName} message on repository queue with archive record id: {ArchiveRecordId}", nameof(IDownloadPackage),
                    downloadPackage.ArchiveRecordId);
            }
        }

        public async Task<int> UpdatePrimaerdatenAuftragStatus(IUpdatePrimaerdatenAuftragStatus newStatus)
        {
            var logId = await auftragAccess.UpdateStatus(new PrimaerdatenAuftragLog
            {
                PrimaerdatenAuftragId = newStatus.PrimaerdatenAuftragId,
                Status = newStatus.Status,
                Service = newStatus.Service,
                ErrorText = newStatus.ErrorText
            }, newStatus.Verarbeitungskanal ?? 0);
            return logId;
        }

        public async Task DeleteOldDownloadAndSyncRecords(int olderThanXDays)
        {
            await auftragAccess.DeleteOldDownloadAndSyncRecords(olderThanXDays);
        }

        public async Task<TestConversionResult> TestConversion()
        {
            const string fileName = "AbbyyTiffTest.tif";

            var assemblyLocation = AppDomain.CurrentDomain.BaseDirectory;
            var img = Resources.AbbyyTiffTest;
            var path = Path.Combine(assemblyLocation, fileName);
            var file = new FileInfo(path);

            if (!file.Exists)
            {
                img.Save(path);
                if (!file.Exists)
                {
                    return new TestConversionResult(false, $"Unable to find file {file.FullName}");
                }
            }

            var targetPath = await renderEngine.ConvertFile(file.FullName, "pdf");
            if (targetPath == file.FullName)
            {
                return new TestConversionResult(false, "No conversion done. Abbyy not installed?");
            }

            return File.Exists(targetPath)
                ? new TestConversionResult(true, "")
                : new TestConversionResult(true, $"Could not find File in path: {targetPath}");
        }


        private void CreateZipFile(string finalZipFolder, string finalZipFile, string tempFolder, bool createWithPassword, string id)
        {
            try
            {
                if (Directory.Exists(finalZipFolder))
                {
                    Directory.Delete(finalZipFolder, true);
                }

                if (File.Exists(finalZipFile))
                {
                    File.Delete(finalZipFile);
                }

                Directory.GetParent(finalZipFolder).Create();
                Directory.Move(tempFolder, finalZipFolder);

                if (createWithPassword)
                {
                    CreateZipFileWithPasswordFromDirectory(finalZipFolder, finalZipFile, passwordHelper.GetHashPassword(id));
                }
                else
                {
                    ZipFile.CreateFromDirectory(finalZipFolder, finalZipFile);
                }
            }
            finally
            {
                if (Directory.Exists(finalZipFolder))
                {
                    Directory.Delete(finalZipFolder, true);
                }
            }
        }

        /// <summary>
        ///     Compresses the files in the nominated folder, and creates a zip file on disk named as destinationArchiveFileName.
        ///     Uses AES 256 encryption.
        /// </summary>
        private void CreateZipFileWithPasswordFromDirectory(string sourceDirectoryName, string destinationArchiveFileName, string password)
        {
            var fsOut = File.Create(destinationArchiveFileName);
            var zipStream = new ZipOutputStream(fsOut);
            zipStream.SetLevel(3); // 0-9, 9 being the highest level of compression
            zipStream.Password = password;

            // As our files can be bigger than 4GB we need to use Zip64 mode.
            // This however prevents the zip to be unpacked by built-in extractor in va, and other older code,
            var di = new DirectoryInfo(sourceDirectoryName);
            var sizeInBytes = di.GetFiles("*.*", SearchOption.AllDirectories).Sum(f => f.Length);
            var larger4Gb = sizeInBytes > 4L * 1000 * 1000 * 1000;
            zipStream.UseZip64 = larger4Gb ? UseZip64.On : UseZip64.Off;

            // This setting will strip the leading part of the folder path in the entries, to
            // make the entries relative to the starting folder.
            // To include the full path for each entry up to the drive root, assign folderOffset = 0.
            var folderOffset = sourceDirectoryName.Length + (sourceDirectoryName.EndsWith("\\") ? 0 : 1);

            CompressFolder(sourceDirectoryName, zipStream, folderOffset);

            zipStream.IsStreamOwner = true; // Makes the Close also Close the underlying stream
            zipStream.Close();
        }

        /// <summary>
        ///     Compresses the folder recursively.
        /// </summary>
        /// <param name="path">The path whose content needs to be compressed.</param>
        /// <param name="zipStream">The zip stream.</param>
        /// <param name="folderOffset">The folder offset.</param>
        private void CompressFolder(string path, ZipOutputStream zipStream, int folderOffset)
        {
            var files = Directory.GetFiles(path);
            foreach (var filename in files)
            {
                var fi = new FileInfo(filename);
                var entryName = filename.Substring(folderOffset); // Makes the name in zip based on the folder
                entryName = ZipEntry.CleanName(entryName); // Removes drive from name and fixes slash direction
                zipStream.PutNextEntry(new ZipEntry(entryName)
                {
                    DateTime = fi.LastWriteTime, // Note the zip format stores 2 second granularity
                    Size = fi.Length,
                    AESKeySize = 256, // Allowable values are 0 (off), 128 or 256
                    IsUnicodeText = true
                });

                // Zip the file in buffered chunks
                // the "using" will close the stream even if an exception occurs
                var buffer = new byte[4096];
                using (var streamReader = File.OpenRead(filename))
                {
                    StreamUtils.Copy(streamReader, zipStream, buffer);
                }

                zipStream.CloseEntry();
            }

            // Compres the folders recursively
            var folders = Directory.GetDirectories(path);
            foreach (var folder in folders)
            {
                CompressFolder(folder, zipStream, folderOffset);
            }
        }

        private async Task<string> ConvertFile(FileInfo file, PaketDIP paket, string tempFolder)
        {
            if (!file.Exists)
            {
                throw new FileNotFoundException($"Unable to find file {file.FullName}", file.FullName);
            }

            var supportedFileTypesForRendering = await renderEngine.GetSupportedFileTypes();
            if (!supportedFileTypesForRendering.Contains(file.Extension.Replace(".", "").ToLowerInvariant()))
            {
                return file.FullName;
            }

            var targetExtension = GetTargetExtension(file);
            var convertedFile = await renderEngine.ConvertFile(file.FullName, targetExtension);
            MetadataXmlUpdater.UpdateFile(file, new FileInfo(convertedFile), paket, tempFolder);
            return convertedFile;
        }

        private static string GetTargetExtension(FileInfo file)
        {
            string targetExtension;
            switch (file.Extension)
            {
                case ".pdf":
                    targetExtension = "pdf";
                    break;
                case ".tif":
                case ".tiff":
                    targetExtension = "pdf";
                    break;
                case ".wav":
                    targetExtension = "mp3";
                    break;
                case ".mp4":
                    targetExtension = "mp4";
                    break;
                default:
                    throw new ArgumentException("Unsupported file extension.");
            }

            return targetExtension;
        }

        private async Task ProcessFolders(List<RepositoryFolder> folders, string path, string archiveRecordId)
        {
            foreach (var repositoryFolder in folders)
            {
                var newPath = Path.Combine(path, repositoryFolder.PhysicalName);
                await ProcessFiles(repositoryFolder.Files, newPath, archiveRecordId);
                await ProcessFolders(repositoryFolder.Folders, newPath, archiveRecordId);
            }
        }

        private async Task ProcessFiles(List<RepositoryFile> files, string path, string archiveRecordId)
        {
            foreach (var repositoryFile in files)
            {
                var diskFile = new FileInfo(Path.Combine(path, repositoryFile.PhysicalName));
                if (repositoryFile.Exported)
                {
                    if (!diskFile.Exists)
                    {
                        Log.Warning("Unable to find file on disk at {diskFile} for {archiveRecordId}", diskFile, archiveRecordId);
                    }

                    // We have found a valid file. Extract the text if the extension is supported
                    var supportedFileTypesForTextExtraction = await textEngine.GetSupportedFileTypes();
                    if (supportedFileTypesForTextExtraction.Contains(diskFile.Extension.Replace(".", "")))
                    {
                        Log.Information("Start extracting text for file: {FullName} for archive record id {archiveRecordId}", diskFile.FullName, archiveRecordId);
                        repositoryFile.ContentText = await textEngine.ExtractText(diskFile.FullName);
                    }
                }
                else
                {
                    Log.Information("Skipping {diskFile} as it was not downloaded from the repository", diskFile);
                }
            }
        }

        private void AddReadmeFile(string tempFolder)
        {
            var pathAndFilename = Path.Combine(tempFolder, "readme.txt");
            var content = parameterHelper.GetSetting<GebrauchskopieSettings>().ReadmeDateiinhalt;

            File.WriteAllText(pathAndFilename, content, Encoding.UTF8);
        }

        private void AddDesignFiles(string tempFolder)
        {
            Log.Information("Adding desing files to usage copy temp directory.");
            var sourcePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Html", "design");
            var destinationPath = Path.Combine(tempFolder, "design");
            if (Directory.Exists(sourcePath))
            {
                // Now Create all of the directories
                foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, destinationPath));
                }

                // Copy all the files & Replaces any files with the same name
                foreach (var newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(sourcePath, destinationPath), true);
                }
            }
            else
            {
                Log.Warning(
                    "Design files for usage copy could not be found. Make sure the files exist in the locaction <AssetManagerDir>\\Html\\design");
            }
        }

        private void ConvertAreldaMetadataXml(string tempFolder)
        {
            Log.Information("Converting arelda metadata.xml file...");

            var metadataFile = Path.Combine(tempFolder, "header", "metadata.xml");
            var transformationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Html", "Xslt", "areldaConvert.xsl");

            // IF one of the files does not exist, log warning and create an "error" index.html file.
            if (!File.Exists(transformationFile) || !File.Exists(metadataFile))
            {
                throw new Exception(
                    $"Could not find the transformation file or the source file to transform. Make sure the both file exists.\nTransformation file: {transformationFile}\nSource file: {metadataFile}");
            }

            var result = transformEngine.TransformXml(metadataFile, transformationFile, null);
            File.WriteAllText(metadataFile, result);
            Log.Information("Converted.");
        }

        private void CreateIndexHtml(string tempFolder, string packageId)
        {
            Log.Information("Creating index.html file.");

            // Get Metadata xml
            var transformationFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Html", "Xslt", "gebrauchskopie.xsl");
            var metadataFile = Path.Combine(tempFolder, "header", "metadata.xml");

            // IF one of the files does not exist, log warning and create an "error" index.html file.
            if (!File.Exists(transformationFile) || !File.Exists(metadataFile))
            {
                Log.Warning(
                    "Could not find the transformation file or the source file to transform. Make sure the both file exists.\n{transformationFile}\n{metadataFile}",
                    transformationFile, metadataFile);
                File.WriteAllText(Path.Combine(tempFolder, "index.html"),
                    "The index.html could not be created. Please inform the Swiss Federal Archive.");
                return;
            }

            // Benutzungskopien have no package id. In that case we pass a null parameter
            var paramCollection = string.IsNullOrEmpty(packageId) ? null : new Dictionary<string, string> {{"packageId", packageId}};
            // Do transformation
            var result = transformEngine.TransformXml(metadataFile, transformationFile, paramCollection);
            File.WriteAllText(Path.Combine(tempFolder, "index.html"), result);
        }

        private long GetProcessingTimeOfIgnoredFilesInTicks(long sizeInBytes)
        {
            if (sizeInBytes <= 0)
            {
                return 0;
            }

            var retVal = 0L;
            var decompressionSpeed = aufbereitungsZeitSettings.DecompressionSpeedInKByte;

            if (decompressionSpeed > 0)
                // ReSharper disable once PossibleLossOfFraction
            {
                retVal += TimeSpan.FromSeconds(sizeInBytes / 1000 / decompressionSpeed).Ticks;
            }

            return retVal;
        }

        private int? GetPriorisierungskategorie(AufbereitungsArtEnum aufbereitungsArt, long sizeInBytes, object workload)
        {
            switch (aufbereitungsArt)
            {
                case AufbereitungsArtEnum.Sync:
                    // This is download
                    Debug.Assert(workload is ArchiveRecordAppendPackage, "Workload must be of type ArchiveRecordAppendPackage");
                    // Vecteur Aufträge sind diejenigen Aufträge, wo die VE bereits im Elastic Index vorhanden ist, aber dort KEINE Primärdaten hat.
                    // Dieser erhalten die Kategorie 2-5. Die anderen Aufträge die Kategorie 6-9
                    var elasticRecord = ((ArchiveRecordAppendPackage) workload).ElasticRecord;
                    if (elasticRecord != null && !elasticRecord.PrimaryData.Any())
                    {
                        return GetPriorisierungskategorie(sizeInBytes);
                    }

                    return GetPriorisierungskategorie(sizeInBytes, 5);
                case AufbereitungsArtEnum.Download:
                    return GetPriorisierungskategorie(sizeInBytes);
                default:
                    throw new ArgumentOutOfRangeException(nameof(aufbereitungsArt), aufbereitungsArt, null);
            }
        }

        private int GetPriorisierungskategorie(long sizeInBytes, int baseKategorie = 1)
        {
            if (sizeInBytes < packageSizeDefinition.MaxSmallSizeInMB * 1048576L)
            {
                return 1 + baseKategorie;
            }

            if (sizeInBytes < packageSizeDefinition.MaxMediumSizeInMB * 1048576L)
            {
                return 2 + baseKategorie;
            }

            if (sizeInBytes < packageSizeDefinition.MaxLargeSizeInMB * 1048576L)
            {
                return 3 + baseKategorie;
            }

            return 4 + baseKategorie;
        }
    }
}