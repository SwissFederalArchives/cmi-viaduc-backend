using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Asset;
using CMI.Contract.Common;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Contract.DocumentConverter;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.Asset;
using CMI.Engine.Asset.PreProcess;
using CMI.Engine.Security;
using CMI.Manager.Asset.ParameterSettings;
using CMI.Manager.Asset.Properties;
using Dasync.Collections;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using MassTransit;
using Newtonsoft.Json;
using Serilog;
using ZipFile = System.IO.Compression.ZipFile;

namespace CMI.Manager.Asset
{
    public class AssetManager : IAssetManager
    {
        private readonly SchaetzungAufbereitungszeitSettings aufbereitungsZeitSettings;
        private readonly IPrimaerdatenAuftragAccess auftragAccess;
        private readonly IBus bus;
        private readonly IRequestClient<FindArchiveRecordRequest> indexClient;
        private readonly AssetPackageSizeDefinition packageSizeDefinition;
        private readonly IParameterHelper parameterHelper;
        private readonly IPdfManipulator pdfManipulator;
        private readonly PasswordHelper passwordHelper;
        private readonly IPreparationTimeCalculator preparationCalculator;
        private readonly IPackagePriorizationEngine priorizationEngine;
        private readonly IRenderEngine renderEngine;
        private readonly ITextEngine textEngine;
        private readonly ITransformEngine transformEngine;


        public AssetManager(ITextEngine textEngine, IRenderEngine renderEngine, ITransformEngine transformEngine, PasswordHelper passwordHelper,
            IParameterHelper parameterHelper, IPdfManipulator pdfManipulator,
            IPreparationTimeCalculator preparationCalculator, IPrimaerdatenAuftragAccess auftragAccess,
            IRequestClient<FindArchiveRecordRequest> indexClient,
            IPackagePriorizationEngine priorizationEngine, IBus bus)
        {
            this.textEngine = textEngine;
            this.renderEngine = renderEngine;
            this.transformEngine = transformEngine;
            this.passwordHelper = passwordHelper;
            this.parameterHelper = parameterHelper;
            this.pdfManipulator = pdfManipulator;
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
        /// <param name="primaerdatenAuftragId">The id number of the PrimaerdatenAuftrag</param>
        /// <returns><c>true</c> if successful, <c>false</c> otherwise.</returns>
        public async Task<bool> ExtractFulltext(long mutationId, ArchiveRecord archiveRecord, int primaerdatenAuftragId)
        {
            var packages = archiveRecord.PrimaryData;
            var processingTimeForMissingFiles = 0L;

            foreach (var repositoryPackage in packages.Where(p => !string.IsNullOrEmpty(p.PackageFileName)))
            {
                var packageFileName = Path.Combine(Settings.Default.PickupPath, repositoryPackage.PackageFileName);
                var fi = new FileInfo(packageFileName);
                var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(), fi.Name.Remove(fi.Name.Length - fi.Extension.Length));
                var watch = Stopwatch.StartNew();

                if (Directory.Exists(tempFolder))
                {
                    Log.Information("Found unzipped files. Starting to process...");
                    var context = new JobContext {ArchiveRecordId = archiveRecord.ArchiveRecordId, PackageId = repositoryPackage.PackageId};
                    var sizeInBytesOnDisk = Directory.GetFiles(tempFolder, "*.*", SearchOption.AllDirectories).Select(f => new FileInfo(f).Length)
                        .Sum();

                    try
                    {
                        await ProcessFiles(repositoryPackage.Files, Path.Combine(tempFolder, "content"), context);
                        await ProcessFolders(repositoryPackage.Folders, Path.Combine(tempFolder, "content"), context);

                        // if we are here everything is okay
                        Log.Information("Successfully processed files (fulltext extracted) from zip file {Name}", fi.Name);
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
                    Log.Warning("Unable to find the unzipped files for {packageFileName}. No text was extracted.", packageFileName);
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
        /// <param name="package">The package to convert</param>
        /// <returns>PackageConversionResult.</returns>
        public async Task<PackageConversionResult> ConvertPackage(string id, AssetType assetType, bool protectWithPassword, RepositoryPackage package)
        {
            var retVal = new PackageConversionResult { Valid = true };
            var packageFileName = Path.Combine(Settings.Default.PickupPath, package.PackageFileName);
            var fi = new FileInfo(packageFileName);

            // Make sure Gebrauchskopien have a packageId
            if (assetType == AssetType.Gebrauchskopie && string.IsNullOrEmpty(package.PackageId))
            {
                throw new InvalidOperationException("Assets of type <Gebrauchskopie> require a packageId");
            }

            if (File.Exists(fi.FullName))
            {
                Log.Information("Found zip file {Name}. File is already unzipped.", fi.Name);
                var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(), fi.Name.Remove(fi.Name.Length - fi.Extension.Length));
                try
                {
                    var metadataFile = Path.Combine(tempFolder, "header", "metadata.xml");
                    var paket = (PaketDIP)Paket.LoadFromFile(metadataFile);

                    var contentFolder = Path.Combine(tempFolder, "content");
                    var context = new JobContext {ArchiveRecordId = package.ArchiveRecordId, PackageId = package.PackageId};
                    await ConvertFiles(id, package.Files, paket, tempFolder, contentFolder, context);
                    await ConvertFolders(id, package.Folders, paket, tempFolder, contentFolder, context);
                    
                    paket.Generierungsdatum = DateTime.Today;
                    ((Paket)paket).SaveToFile(metadataFile);

                    AddReadmeFile(tempFolder);
                    AddDesignFiles(tempFolder);
                    CreateIndexHtml(tempFolder, package.PackageId);

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

            Log.Verbose("Asset for VE {VEID} is NOT in preparationQueue.", archiveRecordId);
            var archiveRecord = await indexClient.GetResponse<FindArchiveRecordResponse>(new FindArchiveRecordRequest { ArchiveRecordId = archiveRecordId });
            var retValue = new PreparationStatus
            {
                AddedToQueueOn = DateTime.MinValue,
                EstimatedPreparationDuration = preparationCalculator.EstimatePreparationDuration(archiveRecord.Message.ElasticArchiveRecord.PrimaryData,
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
                            "Unexpected error while pushing new jobs into the PrimaerdatenAuftrag table or pushing the items on the queue. Resetting PrimaerdatenAuftragStatus");
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
            if (newStatus.PrimaerdatenAuftragId > 0)
            {
                Log.Information("Auftrag mit Id {PrimaerdatenAuftragId} wurde im {service}-Service auf Status {Status} gesetzt.",
                    newStatus.PrimaerdatenAuftragId, newStatus.Service.ToString(), newStatus.Status.ToString());

                var logId = await auftragAccess.UpdateStatus(new PrimaerdatenAuftragLog
                {
                    PrimaerdatenAuftragId = newStatus.PrimaerdatenAuftragId,
                    Status = newStatus.Status,
                    Service = newStatus.Service,
                    ErrorText = newStatus.ErrorText
                }, newStatus.Verarbeitungskanal ?? 0);
                return logId;
            }

            return -1;
        }

        public async Task DeleteOldDownloadAndSyncRecords(int olderThanXDays)
        {
            await auftragAccess.DeleteOldDownloadAndSyncRecords(olderThanXDays);
        }

        public async Task<bool> ExtractZipFile(ExtractZipArgument extractZipArgument)
        {
            var primaerdatenAuftragId = extractZipArgument.PrimaerdatenAuftragId;

            var packageFileName = Path.Combine(Settings.Default.PickupPath, extractZipArgument.PackageFileName);
            var fi = new FileInfo(packageFileName);

            if (File.Exists(fi.FullName))
            {
                Log.Information("Found zip file {Name}. Starting to extract...", fi.Name);
                var tempFolder = Path.Combine(fi.DirectoryName ?? throw new InvalidOperationException(), fi.Name.Remove(fi.Name.Length - fi.Extension.Length));
                try
                {
                    ZipFile.ExtractToDirectory(packageFileName, tempFolder);

                    // Primaerdatenauftrag could be 0 if we have a Benutzungskopie
                    if (primaerdatenAuftragId > 0)
                    {

                        var status = new UpdatePrimaerdatenAuftragStatus
                        {
                            PrimaerdatenAuftragId = primaerdatenAuftragId,
                            Service = AufbereitungsServices.AssetService,
                            Status = AufbereitungsStatusEnum.ZipEntpackt
                        };
                        await UpdatePrimaerdatenAuftragStatus(status);
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error while unzipping package {packageFileName}. Error Message is: {Message}", packageFileName, ex.Message);
                    return false;
                }
            }

            Log.Warning("Unable to find the zip file for {packageFileName}. Nothing was unzipped.", packageFileName);
            return false;
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

                Directory.GetParent(finalZipFolder)?.Create();
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

            // Compress the folders recursively
            var folders = Directory.GetDirectories(path);
            foreach (var folder in folders)
            {
                CompressFolder(folder, zipStream, folderOffset);
            }
        }

        private async Task ConvertFolders(string id, List<RepositoryFolder> folders, PaketDIP paket, string rootFolder, string tempFolder, JobContext context)
        {
            foreach (var repositoryFolder in folders)
            {
                var newPath = Path.Combine(tempFolder, repositoryFolder.PhysicalName);
                await ConvertFiles(id, repositoryFolder.Files, paket, rootFolder, newPath, context);
                await ConvertFolders(id, repositoryFolder.Folders, paket, rootFolder, newPath, context);
            }
        }

        private async Task ConvertFiles(string id, List<RepositoryFile> files, PaketDIP paket, string rootFolder, string tempFolder, JobContext context)
        {
            // Skip empty collections
            if (files.Count == 0)
            {
                return;
            }

            // Create the list with conversion files.
            // This list will contain the splitted file names for processing
            // This list does not contain files that didn't have the flag exported or should be skipped
            var conversionFiles = pdfManipulator.ConvertToConversionFiles(files.ToList(), tempFolder, true);

            var sw = new Stopwatch();
            sw.Start();
            var parallelism = Settings.Default.DocumentTransformParallelism;
            Log.Information("Starting parallel document transform for-each-loop with parallelism of {parallelism} for {Count} files of archiveRecordId or orderId {id}",
                parallelism, files.Count, id);
            var supportedFileTypesForRendering = await renderEngine.GetSupportedFileTypes();


            await conversionFiles.ParallelForEachAsync(async conversionFile =>
            {
                var file = new FileInfo(conversionFile.FullName);
                Log.Information("Start conversion for file: {file} for archive record or order id {id}", file, id);
                conversionFile.ConvertedFile = await ConvertFile(file, supportedFileTypesForRendering, context);
            }, parallelism, true);

            // Now stich back files that were possibly splitted
            pdfManipulator.MergeSplittedFiles(conversionFiles);

            // Update the metadata.xml for all the converted files
            // As speed is not an issue, we're not doing it in parallel
            foreach(var conversionFile in conversionFiles)
            {
                var file = new FileInfo(conversionFile.FullName);
                if (string.IsNullOrEmpty(conversionFile.ParentId))
                {
                    MetadataXmlUpdater.UpdateFile(file, new FileInfo(conversionFile.ConvertedFile), paket, rootFolder);
                }
                
                // Delete the original file, if the convertedFile exists and is not the same as the original file.
                // In case of PDF the name of the original and converted file could be the same. --> PDF to PDF with OCR
                if (file.Exists && conversionFile.ConvertedFile != file.FullName)
                {
                    file.Delete();
                }
            }

            sw.Stop();
            Log.Information("Finished parallel document transform for-each-loop with parallelism of {parallelism} for {Count} files of archiveRecordId or orderId {id} in {TotalSeconds}",
                parallelism, files.Count, id, sw.Elapsed.TotalSeconds);
        }

        private async Task<string> ConvertFile(FileInfo file, string[] supportedFileTypesForRendering, JobContext context)
        {
            if (!file.Exists)
            {
                throw new FileNotFoundException($"Unable to find file {file.FullName}", file.FullName);
            }

            if (!supportedFileTypesForRendering.Contains(file.Extension.Replace(".", "").ToLowerInvariant()))
            {
                return file.FullName;
            }

            var targetExtension = GetTargetExtension(file);
            var convertedFile = await renderEngine.ConvertFile(file.FullName, targetExtension, context);
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

        private async Task ProcessFolders(List<RepositoryFolder> folders, string path, JobContext context)
        {
            foreach (var repositoryFolder in folders)
            {
                var newPath = Path.Combine(path, repositoryFolder.PhysicalName);
                await ProcessFiles(repositoryFolder.Files, newPath, context);
                await ProcessFolders(repositoryFolder.Folders, newPath, context);
            }
        }

        private async Task ProcessFiles(List<RepositoryFile> files, string path, JobContext context)
        {
            // Skip empty directories
            if (files.Count == 0)
            {
                return;
            }

            var supportedFileTypesForTextExtraction = await textEngine.GetSupportedFileTypes();

            // Create the list with the text extraction files.
            // This list will contain the splitted file names for processing
            // This list does not contain files that didn't have the flag exported or should be skipped
            var textExtractionFiles = pdfManipulator.ConvertToTextExtractionFiles(files, path);

            var sw = new Stopwatch();
            sw.Start();
            var parallelism = Settings.Default.TextExtractParallelism;
            Log.Information("Starting parallel ocr extraction for-each-loop with parallelism of {parallelism} for {Count} files of archiveRecordId {archiveRecord}",
                parallelism, files.Count, context.ArchiveRecordId);

            await textExtractionFiles.ParallelForEachAsync(async textExtractionFile =>
            {
                var diskFile = new FileInfo(textExtractionFile.FullName);
                if (!diskFile.Exists)
                {
                    Log.Warning("Unable to find file on disk at {diskFile} for {archiveRecordId}", diskFile, context.ArchiveRecordId);
                }

                // We have found a valid file. Extract the text if the extension is supported
                if (supportedFileTypesForTextExtraction.Contains(diskFile.Extension.Replace(".", "")))
                {
                    Log.Information("Start extracting text for file: {FullName} for archive record id {archiveRecordId} on thread {threadId}", diskFile.FullName,
                        context.ArchiveRecordId, Thread.CurrentThread.ManagedThreadId);
                    textExtractionFile.ContentText = await textEngine.ExtractText(diskFile.FullName, context);
                }
            }, parallelism, true);

            // Now convert the extracted texts back to the original repository files
            pdfManipulator.TransferExtractedText(textExtractionFiles, files);

            sw.Stop();
            Log.Information("Finished parallel ocr extraction for-each-loop with parallelism of {parallelism} for {Count} files of archiveRecordId {archiveRecord} in {TotalSeconds}",
                parallelism, files.Count, context.ArchiveRecordId, sw.Elapsed.TotalSeconds);
            Log.Debug(JsonConvert.SerializeObject(files));

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
            var paramCollection = string.IsNullOrEmpty(packageId) ? null : new Dictionary<string, string> { { "packageId", packageId } };
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
                    var elasticRecord = ((ArchiveRecordAppendPackage)workload).ElasticRecord;
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