using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using CMI.Access.Repository;
using CMI.Contract.Common;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Contract.Messaging;
using CMI.Contract.Repository;
using DotCMIS.Client;
using DotCMIS.Data.Extensions;
using MassTransit;
using Newtonsoft.Json;
using Serilog;

namespace CMI.Engine.PackageMetadata
{
    public class PackageHandler : IPackageHandler
    {
        private const string subFondsLevelIdentifier = "Teilbestand";
        private readonly IRequestClient<GetArchiveRecordsForPackageRequest> indexClient;
        private readonly IMetadataDataAccess metadataAccess;
        private readonly IRepositoryDataAccess repositoryAccess;
        private IFolder entryFolder; // This is the folder the user requested. Metadatea can be above and below this folder


        public PackageHandler(IRepositoryDataAccess repositoryAccess, IMetadataDataAccess metadataAccess,
            IRequestClient<GetArchiveRecordsForPackageRequest> indexClient)
        {
            this.repositoryAccess = repositoryAccess;
            this.metadataAccess = metadataAccess;
            // Needing the bus here, is not ideal. But the bus is the only way to access data in the SSZ zone from the BV zone
            this.indexClient = indexClient;
            FoldersTreeList = new FolderInfoList();
        }

        /// <summary>
        ///     Gets a list with folders ordered by descending level.
        ///     Order Ablieferung/Ordnungssystemposition/[Ordnungssystemposition]/Dossier/[Dossier]/Dokument
        /// </summary>
        /// <value>The folders ordered by level.</value>
        public FolderInfoList FoldersTreeList { get; }


        public async Task CreateMetadataXml(string folderName, RepositoryPackage package, List<RepositoryFile> filesToIgnore)
        {
            // Make sure folder name exists
            if (!Directory.Exists(folderName))
            {
                Directory.CreateDirectory(folderName);
            }

            CopyXsdFiles(folderName);

            // Lesen der Bestellposition und aller Kinder aus dem Elastic Index. 
            var getArchiveRecordsForPackageRequest = new GetArchiveRecordsForPackageRequest { PackageId = package.PackageId };
            var response = await indexClient.GetResponse<GetArchiveRecordsForPackageResponse>(getArchiveRecordsForPackageRequest);
            
            var indexRecords = response.Message?.Result;
            Log.Debug($"Found the following archive records for pack" +
                      $"ageId {package.PackageId}: {JsonConvert.SerializeObject(indexRecords)}");

            // If using the Alfresco Repository, then we simply return a "hard coded" file
            if (repositoryAccess.GetRepositoryName().StartsWith("Alfresco", StringComparison.InvariantCultureIgnoreCase))
            {
                var defaultMetadata = GetFileFromRessource();
                File.WriteAllText(Path.Combine(folderName, "metadata.xml"), defaultMetadata);
                return;
            }

            // Einlesen aller Folder Objekte aus dem DIR. Einerseit vom Einstiegspunkt hinauf bis zur Ablieferung
            // andererseits nach unten bis in die tiefste Ebene.
            InitFolders(package);

            var dip = new PaketDIP
            {
                SchemaVersion = SchemaVersion.Item41,
                Generierungsdatum = DateTime.Today,
                Bestellinformation = null // Info nur für Benutzungskopie von Vecteur
            };

            // Schreibe die Daten in die Schema-Struktur, ausgehend vom "obersten" Ordner, der Ablieferung.
            var root = FoldersTreeList.Find(f => f.Parent == null);
            if (root == null)
            {
                throw new InvalidOperationException("Unable to find root folder for exporting metadata.");
            }

            AddFolderData(root, null, dip, indexRecords.ToList(), package.PackageId, filesToIgnore);


            // Generiere noch das Inhaltsverzeichnis
            var contentRoot = new OrdnerDIP
            {
                Id = $"contentRoot{DateTime.Now.Ticks}",
                Name = "content",
                OriginalName = "content"
            };
            dip.Inhaltsverzeichnis.Ordner.Add(contentRoot);
            foreach (var folder in package.Folders)
            {
                ProcessFolder(contentRoot.Ordner, folder);
            }

            ProcessFiles(contentRoot.Datei, package.Files);


            // Save data to disk
            dip.SchemaLocation = "http://bar.admin.ch/gebrauchskopie/v1 gebrauchskopie.xsd";
            ((Paket) dip).SaveToFile(Path.Combine(folderName, "metadata.xml"));
        }


        /// <summary>
        ///     Initializes the folders in a helper collection.
        ///     From the entry point we traverse up to the "Ablieferungen" node
        ///     and down accoring to the information in the package.
        /// </summary>
        /// <param name="package">The package.</param>
        private void InitFolders(RepositoryPackage package)
        {
            Log.Verbose("Initializing folders for package {packageId}", package.PackageId);
            var entryPoint = repositoryAccess.GetRepositoryRoot(package.PackageId);
            entryFolder = repositoryAccess.GetCmisFolder(entryPoint.Id);

            // Traverse each parent and create a info folder object
            Log.Verbose("Traverse up from the entry folder");
            var cmisFolder = entryFolder;
            FolderInfo previous = null;
            while (cmisFolder.FolderParent != null)
            {
                var addedFolder = AddFolderToTreeList(cmisFolder, true, cmisFolder == entryFolder);

                // Add the new folder as a parent of the previous one
                // (remember we traverse up the hierachy here)
                if (previous != null)
                {
                    previous.Parent = addedFolder;
                }

                // When we reach "Ablieferungen" then we can stop
                if (addedFolder.FolderType == PackageFolderType.Ablieferung)
                {
                    break;
                }

                cmisFolder = cmisFolder.FolderParent;
                previous = addedFolder;
            }

            // And now traverse down and the lower folders
            Log.Verbose("Traverse down from the entry folder");
            var parent = FoldersTreeList.Last();
            AddRepositoryFoldersToTreeList(package.Folders, parent);

            Log.Verbose("Finished to initialize folder tree. Data is {FoldersTreeList}", JsonConvert.SerializeObject(FoldersTreeList));
        }

        /// <summary>
        ///     Adds the folder data to the DIP package.
        /// </summary>
        /// <param name="folder">The folder to add.</param>
        /// <param name="parent">The parent under which the new folder should be added.</param>
        /// <param name="dip">The existing dip object where the data is added.</param>
        /// <param name="indexRecords">
        ///     A list with the metadata information from the elastic index containing the ordered archive record and all its
        ///     children.
        ///     The first record in the collection is the ordered archive record
        /// </param>
        /// <param name="packageId">The packageId of the ordered item</param>
        /// <param name="filesToIgnore">A list of files that should not be included in the output</param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        private void AddFolderData(FolderInfo folder, FolderInfo parent, PaketDIP dip, List<ElasticArchiveRecord> indexRecords, string packageId,
            List<RepositoryFile> filesToIgnore)
        {
            var extensions = folder.CmisFolder.GetExtensions(ExtensionLevel.Object);
            Log.Verbose("Adding folder data to DIP Package for {FolderType}: {data} with parent {parent}", folder.FolderType,
                JsonConvert.SerializeObject(extensions), parent);
            switch (folder.FolderType)
            {
                case PackageFolderType.Ablieferung:
                    var orderedRecord = indexRecords.FirstOrDefault(i => i.PrimaryDataLink == packageId);
                    AddAblieferungData(dip.Ablieferung, extensions, orderedRecord);
                    break;
                case PackageFolderType.OrdnungssystemPosition:
                    OrdnungssystempositionDIP position;
                    switch (parent.FolderType)
                    {
                        case PackageFolderType.Ablieferung:
                            position = AddOrdnungssystemPositionData(dip.Ablieferung.Ordnungssystem.Ordnungssystemposition, extensions);
                            break;
                        case PackageFolderType.OrdnungssystemPosition:
                            var parentPosition = FindOrdnungssystemPositionInPackage(parent, dip);
                            position = AddOrdnungssystemPositionData(parentPosition.Ordnungssystemposition, extensions);
                            break;
                        default:
                            throw new InvalidOperationException(
                                "A <Ordnungssystemposition> can only be added to a <Ordnungssystem> or another <Ordnungssystemposition>.");
                    }

                    folder.Id = position.Id;
                    break;
                case PackageFolderType.Dossier:
                    DossierDIP dossier;
                    var dossierRecord = GetArchiveRecordFromDossier(folder, indexRecords);
                    switch (parent.FolderType)
                    {
                        case PackageFolderType.OrdnungssystemPosition:
                            var parentPosition = FindOrdnungssystemPositionInPackage(parent, dip);
                            dossier = AddDossierData(parentPosition.Dossier, extensions, dossierRecord,
                                folder.IsOrderedItem || folder.IsChildOfOrderedItem, filesToIgnore);
                            break;
                        case PackageFolderType.Dossier:
                            var parentDossier = FindDossierInPackage(parent, dip);
                            dossier = AddDossierData(parentDossier.Dossier, extensions, dossierRecord,
                                folder.IsOrderedItem || folder.IsChildOfOrderedItem, filesToIgnore);
                            break;
                        default:
                            throw new InvalidOperationException("A <Dossier> can only be added to a <Ordnungssystemposition> or another <Dossier>.");
                    }

                    folder.Id = dossier.Id;
                    break;
                case PackageFolderType.Dokument:
                    DokumentDIP dokument;
                    var documentRecord = GetArchiveRecordFromDocument(folder, indexRecords);
                    switch (parent.FolderType)
                    {
                        case PackageFolderType.Dossier:
                            var parentDossier = FindDossierInPackage(parent, dip);
                            dokument = AddDokumentData(parentDossier, extensions, documentRecord, filesToIgnore);
                            break;
                        default:
                            throw new InvalidOperationException("A <Dokument> can only be added to a <Dossier>.");
                    }

                    folder.Id = dokument.Id;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            foreach (var child in FoldersTreeList.GetChildren(folder))
            {
                AddFolderData(child, folder, dip, indexRecords, packageId, filesToIgnore);
            }
        }

        /// <summary>
        ///     Gets the archive record from the indexRecords collection that matches the given dossier.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="indexRecords">The index records.</param>
        /// <returns>ElasticArchiveRecord.</returns>
        /// <exception cref="ArgumentOutOfRangeException">folder - Folder must be of type 'Dossier'</exception>
        private ElasticArchiveRecord GetArchiveRecordFromDossier(FolderInfo folder, List<ElasticArchiveRecord> indexRecords)
        {
            if (folder.FolderType != PackageFolderType.Dossier)
            {
                throw new ArgumentOutOfRangeException(nameof(folder), "Folder must be of type 'Dossier'");
            }

            var extensions = folder.CmisFolder.GetExtensions(ExtensionLevel.Object);

            var packageId = metadataAccess.GetExtendedPropertyValue(extensions, "AIP-ID_Dossier-ID");
            var archiveRecord = indexRecords.FirstOrDefault(a => a.PrimaryDataLink.Equals(packageId, StringComparison.InvariantCultureIgnoreCase));
            if (archiveRecord != null)
            {
                Log.Verbose("Found archive record to dossier folder with id {id}. Archive record id is {ArchiveRecordId}", folder.Id,
                    archiveRecord.ArchiveRecordId);
            }
            else
            {
                Log.Verbose("Unable to find archive record to dossier folder with id {id}.", folder.Id);
            }

            return archiveRecord;
        }

        /// <summary>
        ///     Gets the archive record from the indexRecords collection that matches the given cmis folder.
        ///     The cmis filder must be of type "document"
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="indexRecords">The index records.</param>
        /// <returns>ElasticArchiveRecord.</returns>
        /// <exception cref="ArgumentOutOfRangeException">folder - Folder must be of type 'Dokument'</exception>
        private ElasticArchiveRecord GetArchiveRecordFromDocument(FolderInfo folder, List<ElasticArchiveRecord> indexRecords)
        {
            if (folder.FolderType != PackageFolderType.Dokument)
            {
                throw new ArgumentOutOfRangeException(nameof(folder), "Folder must be of type 'Dokument'");
            }

            // Documents in CMIS system don't have a direct access identifier like dossiers. They are combined
            // with the AIP@Dossier_ID from the parent dossier with the id of the document.
            // But to lookup the record, we can use the documentId as this id must be unique within a package
            var extensions = folder.CmisFolder.GetExtensions(ExtensionLevel.Object);
            var dokumentId = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dokument/Dokument@id");
            Log.Verbose("Found the id of the document:  {id}", dokumentId);

            var archiveRecord = indexRecords.FirstOrDefault(a => a.PrimaryDataLink.EndsWith(dokumentId, StringComparison.InvariantCultureIgnoreCase));
            if (archiveRecord != null)
            {
                Log.Verbose("Found archive record to document folder with id {id}. Archive record id is {ArchiveRecordId}", folder.Id,
                    archiveRecord.ArchiveRecordId);
            }
            else
            {
                Log.Verbose("Unable to find archive record to document folder with id {id}.", folder.Id);
            }

            return archiveRecord;
        }


        public OrdnungssystempositionDIP FindOrdnungssystemPositionInPackage(FolderInfo folder, PaketDIP dip)
        {
            Log.Verbose("Trying to find ordnungssystem position in dip package for folder with Id {Id}", folder.Id);
            var allPositions = dip.Ablieferung.Ordnungssystem.Ordnungssystemposition.Traverse(p => p.Ordnungssystemposition).ToList();
            var found = allPositions.FirstOrDefault(p => p.Id == folder.Id);
            Log.Verbose("Found this item: {found}", JsonConvert.SerializeObject(found));
            return found;
        }

        public DossierDIP FindDossierInPackage(FolderInfo folder, PaketDIP dip)
        {
            Log.Verbose("Trying to find dossier in dip package for folder with Id {Id}", folder.Id);
            var allDossiers = dip.Ablieferung.Ordnungssystem.Ordnungssystemposition.Traverse(p => p.Ordnungssystemposition).SelectMany(p => p.Dossier)
                .Traverse(p => p.Dossier);
            var found = allDossiers.FirstOrDefault(p => p.Id == folder.Id);
            Log.Verbose("Found this item: {found}", JsonConvert.SerializeObject(found));
            return found;
        }


        private void AddRepositoryFoldersToTreeList(List<RepositoryFolder> repositoryFolders, FolderInfo parent)
        {
            foreach (var repositoryFolder in repositoryFolders)
            {
                Log.Verbose("Add folder with logical name {LogicalName}", repositoryFolder.LogicalName);
                var cmisFolder = repositoryAccess.GetCmisFolder(repositoryFolder.Id);
                var addedFolder = AddFolderToTreeList(cmisFolder, false, false);
                addedFolder.Parent = parent;

                Log.Verbose("Adding children if any.");
                AddRepositoryFoldersToTreeList(repositoryFolder.Folders, addedFolder);
            }
        }

        private FolderInfo AddFolderToTreeList(IFolder cmisFolder, bool addToStart, bool isOrderedItem)
        {
            var type = metadataAccess.GetExtendedPropertyValue(cmisFolder.GetExtensions(ExtensionLevel.Object), "type");
            if (Enum.TryParse(type, true, out PackageFolderType cmisType))
            {
                var folderInfo = new FolderInfo
                {
                    Id = cmisFolder.Id,
                    CmisFolder = cmisFolder,
                    FolderType = cmisType,
                    IsOrderedItem = isOrderedItem
                };

                if (addToStart)
                {
                    folderInfo.IsChildOfOrderedItem = false;
                    FoldersTreeList.Insert(0, folderInfo);
                }
                else
                {
                    folderInfo.IsChildOfOrderedItem = true;
                    FoldersTreeList.Add(folderInfo);
                }

                return folderInfo;
            }

            Log.Warning("Found unknown folder type or could not parse value: {type}.", type);

            return null;
        }

        private void AddAblieferungData(AblieferungDIP ablieferung, IList<ICmisExtensionElement> extensions, ElasticArchiveRecord orderedRecord)
        {
            Log.Verbose("Adding Ablieferung data with metadata: {extensions}", JsonConvert.SerializeObject(extensions));
            if (ablieferung != null)
            {
                ablieferung.Ablieferungsnummer =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/ablieferungsnummer");
                ablieferung.Ablieferungstyp =
                    Enum.TryParse(metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/ablieferungstyp"), true,
                        out Ablieferungstyp typ)
                        ? typ
                        : Ablieferungstyp.FILES;
                ablieferung.AblieferndeStelle =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/ablieferndeStelle");
                ablieferung.Entstehungszeitraum =
                    metadataAccess.GetHistorischerZeitraum(extensions, "ARELDA:Ablieferung/ablieferung/entstehungszeitraum");
                ablieferung.Bemerkung = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/bemerkung");


                // Provenienz
                ablieferung.Provenienz.AktenbildnerName =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/provenienz/aktenbildnerName");
                ablieferung.Provenienz.SystemName =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/provenienz/systemName");
                ablieferung.Provenienz.SystemBeschreibung =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/provenienz/systemBeschreibung");
                ablieferung.Provenienz.VerwandteSysteme =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/provenienz/verwandteSysteme");
                ablieferung.Provenienz.ArchivierungsmodusLoeschvorschriften = metadataAccess.GetExtendedPropertyValue(extensions,
                    "ARELDA:Ablieferung/ablieferung/provenienz/archivierungsmodusLoeschvorschriften");
                ablieferung.Provenienz.Registratur =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/provenienz/registratur");

                // Ordnungssystem
                var teilbestand = orderedRecord.ArchiveplanContext.FirstOrDefault(c =>
                    c.Level.Equals(subFondsLevelIdentifier, StringComparison.InvariantCultureIgnoreCase));
                ablieferung.Ordnungssystem.Name = teilbestand != null
                    ? teilbestand.Title
                    : metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/ordnungssystem/name");
                ablieferung.Ordnungssystem.Generation =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/ordnungssystem/Generation");
                ablieferung.Ordnungssystem.Mitbenutzung =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/ordnungssystem/Mitbenutzung");
                ablieferung.Ordnungssystem.Bemerkung =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ablieferung/ablieferung/ordnungssystem/Bemerkung");
            }
        }

        private OrdnungssystempositionDIP AddOrdnungssystemPositionData(IList<OrdnungssystempositionDIP> ordnungssystempositionCollection,
            IList<ICmisExtensionElement> extensions)
        {
            Log.Verbose("Adding Ordnungssystemposition data with metadata: {extensions}", JsonConvert.SerializeObject(extensions));

            var position = new OrdnungssystempositionDIP
            {
                Nummer = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ordnungssystemposition/Ordnungssystemposition/Nummer"),
                Titel = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ordnungssystemposition/Ordnungssystemposition/Titel"),
                Id = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ordnungssystemposition/Ordnungssystemposition@id"),
                FederfuehrendeOrganisationseinheit = metadataAccess.GetExtendedPropertyValue(extensions,
                    "ARELDA:Ordnungssystemposition/Ordnungssystemposition/FederfuehrendeOrganisationseinheit"),
                Klassifizierungskategorie = metadataAccess.GetExtendedPropertyValue(extensions,
                    "ARELDA:Ordnungssystemposition/Ordnungssystemposition/Klassifizierungskategorie"),
                Datenschutz = bool.TryParse(
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Ordnungssystemposition/Ordnungssystemposition/Datenschutz"),
                    out var datanschutz) && datanschutz,
                Oeffentlichkeitsstatus = metadataAccess.GetExtendedPropertyValue(extensions,
                    "ARELDA:Ordnungssystemposition/Ordnungssystemposition/Oeffentlichkeitsstatus"),
                OeffentlichkeitsstatusBegruendung = metadataAccess.GetExtendedPropertyValue(extensions,
                    "ARELDA:Ordnungssystemposition/Ordnungssystemposition/OeffentlichkeitsstatusBegruendung")
            };

            ordnungssystempositionCollection.Add(position);
            return position;
        }

        private DossierDIP AddDossierData(IList<DossierDIP> dossierCollection, IList<ICmisExtensionElement> extensions,
            ElasticArchiveRecord dossierRecord, bool addDateiRefList, List<RepositoryFile> filesToIgnore)
        {
            Log.Verbose("Adding Dossier data with metadata: {extensions} and index record customfields {dossierRecord}",
                JsonConvert.SerializeObject(extensions), JsonConvert.SerializeObject(dossierRecord?.CustomFields));

            var dossier = new DossierDIP
            {
                Aktenzeichen = string.IsNullOrEmpty(dossierRecord?.Aktenzeichen())
                    ? metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/Aktenzeichen")
                    : dossierRecord.Aktenzeichen(),
                Zusatzmerkmal = string.IsNullOrEmpty(dossierRecord?.Zusatzmerkmal())
                    ? metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/Zusatzmerkmal")
                    : dossierRecord.Zusatzmerkmal(),
                Titel = string.IsNullOrEmpty(dossierRecord?.Title)
                    ? metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/Titel")
                    : dossierRecord.Title,
                Inhalt = string.IsNullOrEmpty(dossierRecord?.WithinInfo)
                    ? metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/Inhalt")
                    : dossierRecord.WithinInfo,
                Id = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:dossier/dossier@id"),
                Erscheinungsform =
                    Enum.TryParse(metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/Erscheinungsform"), true,
                        out ErscheinungsformDossier ef)
                        ? ef
                        : ErscheinungsformDossier.keineAngabe,
                Umfang = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/Umfang"),
                FederfuehrendeOrganisationseinheit =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/FederfuehrendeOrganisationseinheit"),
                Eroeffnungsdatum = metadataAccess.GetHistorischerZeitpunkt(extensions, "ARELDA:Dossier/Dossier/Eroeffnungsdatum"),
                Abschlussdatum = metadataAccess.GetHistorischerZeitpunkt(extensions, "ARELDA:Dossier/Dossier/Abschlussdatum"),
                Entstehungszeitraum = dossierRecord?.CreationPeriod == null
                    ? metadataAccess.GetHistorischerZeitraum(extensions, "ARELDA:Dossier/Dossier/Entstehungszeitraum")
                    : GetEntstehungszeitraum(dossierRecord.CreationPeriod),
                EntstehungszeitraumAnmerkung = string.IsNullOrEmpty(dossierRecord?.EntstehungszeitraumAnmerkung())
                    ? metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/EntstehungszeitraumAnmerkung")
                    : dossierRecord.EntstehungszeitraumAnmerkung(),
                Klassifizierungskategorie = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/Klassifizierungskategorie"),
                Datenschutz = bool.TryParse(metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/Datenschutz"),
                    out var datenschutz) && datenschutz,
                Oeffentlichkeitsstatus = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/Oeffentlichkeitsstatus"),
                OeffentlichkeitsstatusBegruendung =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/OeffentlichkeitsstatusBegruendung"),
                SonstigeBestimmungen = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/SonstigeBestimmungen"),
                // ToDO: In order to handle Vorgang we would need code that can handle collections. Currently we don't need that property.
                // Vorgang = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/Vorgang"),  
                Bemerkung = string.IsNullOrEmpty(dossierRecord?.ZusätzlicheInformationen())
                    ? metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dossier/Dossier/Bemerkung")
                    : dossierRecord.ZusätzlicheInformationen(),
                DateiRef = addDateiRefList
                    ? GetFilteredDateiRef(metadataAccess.GetExtendedPropertyValues(extensions, "ARELDA:Dossier/Dossier/DateiRef"), filesToIgnore)
                    : null
            };

            // Add optional data
            if (!string.IsNullOrEmpty(dossierRecord?.ReferenceCode))
            {
                dossier.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Signatur", Value = dossierRecord.ReferenceCode});
            }

            if (!string.IsNullOrEmpty(dossierRecord?.Level))
            {
                dossier.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Stufe", Value = dossierRecord.Level});
            }

            if (!string.IsNullOrEmpty(dossierRecord?.FormerReferenceCode))
            {
                dossier.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Frühere Signaturen", Value = dossierRecord.FormerReferenceCode});
            }

            if (dossierRecord?.ArchiveplanContext.Count > 0)
            {
                dossier.zusatzDaten.Add(new ZusatzDatenMerkmal
                    {Name = "Archivplankontext", Value = JsonConvert.SerializeObject(dossierRecord.ArchiveplanContext)});
            }

            if (!string.IsNullOrEmpty(dossierRecord?.Land()))
            {
                dossier.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Land", Value = dossierRecord.Land()});
            }

            if (!string.IsNullOrEmpty(dossierRecord?.Form()))
            {
                dossier.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Form", Value = dossierRecord.Form()});
            }

            if (!string.IsNullOrEmpty(dossierRecord?.FrüheresAktenzeichen()))
            {
                dossier.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Früheres Aktenzeichen", Value = dossierRecord.FrüheresAktenzeichen()});
            }

            if (!string.IsNullOrEmpty(dossierRecord?.PrimaryDataLink))
            {
                dossier.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Identifikation digitales Magazin", Value = dossierRecord.PrimaryDataLink});
            }

            if (dossierRecord?.CreationPeriod != null)
            {
                dossier.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Entstehungszeitraum Anzeigetext", Value = dossierRecord.CreationPeriod.Text});
            }

            var reihenfolge =
                metadataAccess.GetExtendedPropertyBagValue(extensions, "ARELDA:Dossier/Dossier/Zusatzdaten/Merkmal", "ReihenfolgeAnalogesDossier");
            if (!string.IsNullOrEmpty(reihenfolge))
            {
                dossier.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "ReihenfolgeAnalogesDossier", Value = reihenfolge});
            }


            dossierCollection.Add(dossier);
            return dossier;
        }

        private List<string> GetFilteredDateiRef(List<string> dateiRefList, List<RepositoryFile> filesToIgnore)
        {
            return dateiRefList.Where(d => !filesToIgnore.Select(f => f.SipId).Contains(d)).ToList();
        }

        private DokumentDIP AddDokumentData(DossierDIP dossier, IList<ICmisExtensionElement> extensions, ElasticArchiveRecord documentRecord,
            List<RepositoryFile> filesToIgnore)
        {
            Log.Verbose("Adding Dokument data with metadata: {extensions}", JsonConvert.SerializeObject(extensions));

            var dokument = new DokumentDIP
            {
                Titel = string.IsNullOrEmpty(documentRecord?.Title)
                    ? metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dokument/Dokument/Titel")
                    : documentRecord.Title,
                Id = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:dokument/dokument@id"),
                Erscheinungsform =
                    Enum.TryParse(metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dokument/Dokument/Erscheinungsform"), true,
                        out ErscheinungsformDokument ef)
                        ? ef
                        : ErscheinungsformDokument.nichtdigital,
                Registrierdatum = metadataAccess.GetHistorischerZeitpunkt(extensions, "ARELDA:Dokument/Dokument/Registrierdatum"),
                Entstehungszeitraum = documentRecord?.CreationPeriod == null
                    ? metadataAccess.GetHistorischerZeitraum(extensions, "ARELDA:Dokument/Dokument/Entstehungszeitraum")
                    : GetEntstehungszeitraum(documentRecord.CreationPeriod),
                Klassifizierungskategorie = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dokument/Dokument/Klassifizierungskategorie"),
                Datenschutz = bool.TryParse(metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dokument/Dokument/Datenschutz"),
                    out var datenschutz) && datenschutz,
                Oeffentlichkeitsstatus = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dokument/Dokument/Oeffentlichkeitsstatus"),
                OeffentlichkeitsstatusBegruendung =
                    metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dokument/Dokument/OeffentlichkeitsstatusBegruendung"),
                SonstigeBestimmungen = metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dokument/Dokument/SonstigeBestimmungen"),
                Bemerkung = string.IsNullOrEmpty(documentRecord?.ZusätzlicheInformationen())
                    ? metadataAccess.GetExtendedPropertyValue(extensions, "ARELDA:Dokument/Dokument/Bemerkung")
                    : documentRecord.ZusätzlicheInformationen(),
                DateiRef = GetFilteredDateiRef(metadataAccess.GetExtendedPropertyValues(extensions, "ARELDA:Dokument/Dokument/DateiRef"),
                    filesToIgnore)
            };

            // Add optional data
            if (!string.IsNullOrEmpty(documentRecord?.ReferenceCode))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Signatur", Value = documentRecord.ReferenceCode});
            }

            if (!string.IsNullOrEmpty(documentRecord?.Level))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Stufe", Value = documentRecord.Level});
            }

            if (!string.IsNullOrEmpty(documentRecord?.FormerReferenceCode))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Frühere Signaturen", Value = documentRecord.FormerReferenceCode});
            }

            if (documentRecord?.ArchiveplanContext.Count > 0)
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal
                    {Name = "Archivplankontext", Value = JsonConvert.SerializeObject(documentRecord.ArchiveplanContext)});
            }

            if (!string.IsNullOrEmpty(documentRecord?.Form()))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Form", Value = documentRecord.Form()});
            }

            if (!string.IsNullOrEmpty(documentRecord?.WithinInfo))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Darin", Value = documentRecord.WithinInfo});
            }

            if (!string.IsNullOrEmpty(documentRecord?.Thema()))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Thema", Value = documentRecord.Thema()});
            }

            if (!string.IsNullOrEmpty(documentRecord?.Format()))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Format", Value = documentRecord.Format()});
            }

            if (!string.IsNullOrEmpty(documentRecord?.Urheber()))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Urheber", Value = documentRecord.Urheber()});
            }

            if (!string.IsNullOrEmpty(documentRecord?.Verleger()))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Verleger", Value = documentRecord.Verleger()});
            }

            if (!string.IsNullOrEmpty(documentRecord?.Abdeckung()))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Abdeckung", Value = documentRecord.Abdeckung()});
            }

            if (!string.IsNullOrEmpty(documentRecord?.PrimaryDataLink))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "Identifikation digitales Magazin", Value = documentRecord.PrimaryDataLink});
            }

            if (documentRecord?.CreationPeriod != null)
            {
                dokument.zusatzDaten.Add(
                    new ZusatzDatenMerkmal {Name = "Entstehungszeitraum Anzeigetext", Value = documentRecord.CreationPeriod.Text});
            }

            var reihenfolge =
                metadataAccess.GetExtendedPropertyBagValue(extensions, "ARELDA:Dokument/Dokument/Zusatzdaten/Merkmal", "ReihenfolgeAnalogesDossier");
            if (!string.IsNullOrEmpty(reihenfolge))
            {
                dokument.zusatzDaten.Add(new ZusatzDatenMerkmal {Name = "ReihenfolgeAnalogesDossier", Value = reihenfolge});
            }


            dossier.Dokument.Add(dokument);
            return dokument;
        }


        /// <summary>
        ///     Processes folders, their subfolders and files for creating the TOC of the DIP package.
        /// </summary>
        /// <param name="ordnerListe">The ordner liste.</param>
        /// <param name="repositoryFolder">The repository folder.</param>
        private void ProcessFolder(List<OrdnerDIP> ordnerListe, RepositoryFolder repositoryFolder)
        {
            var ordner = new OrdnerDIP
            {
                Id = $"{repositoryFolder.SipId}_D", // Adding suffix _D so we have unique names. Otherwise the folder has the same id like the dossier
                Name = repositoryFolder.PhysicalName,
                OriginalName = repositoryFolder.LogicalName
            };
            ProcessFiles(ordner.Datei, repositoryFolder.Files);
            foreach (var folder in repositoryFolder.Folders)
            {
                ProcessFolder(ordner.Ordner, folder);
            }

            ordnerListe.Add(ordner);
        }

        private void ProcessFiles(List<DateiDIP> dateiListe, List<RepositoryFile> packageFiles)
        {
            foreach (var file in packageFiles)
            {
                var datei = new DateiDIP
                {
                    Name = file.LogicalName,
                    OriginalName = file.SipOriginalName,
                    Id = file.SipId,
                    Pruefalgorithmus = MapHashType(file.HashAlgorithm),
                    Pruefsumme = file.Hash
                };

                dateiListe.Add(datei);
            }
        }

        /// <summary>
        ///     Maps the repository value for the hash type to our enumeration
        /// </summary>
        /// <param name="hashType">Type of the hash.</param>
        /// <returns>CMI.Manager.Repository.RepositoryHashAlgorithmType.</returns>
        private Pruefalgorithmus MapHashType(string hashType)
        {
            switch (hashType.ToLowerInvariant())
            {
                case "1":
                case "md5":
                    return Pruefalgorithmus.MD5;
                case "2":
                case "sha-1":
                    return Pruefalgorithmus.SHA1;
                case "3":
                case "sha-256":
                    return Pruefalgorithmus.SHA256;
                case "4":
                case "sha-512":
                    return Pruefalgorithmus.SHA512;
                default:
                    return Pruefalgorithmus.MD5;
            }
        }

        private string GetFileFromRessource()
        {
            var resourceName = "CMI.Engine.PackageMetadata.DefaultAlfrescoMetadataFile.xml";
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                using (var reader = new StreamReader(stream ?? throw new InvalidOperationException(), Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private void CopyXsdFiles(string outputDirectory)
        {
            var executingDir = AppDomain.CurrentDomain.BaseDirectory;
            var xsdDir = Path.Combine(executingDir, "xsd");
            foreach (var file in Directory.GetFiles(xsdDir, "*.xsd"))
            {
                var fi = new FileInfo(file);
                fi.CopyTo(Path.Combine(outputDirectory, fi.Name), true);
            }
        }

        private HistorischerZeitraum GetEntstehungszeitraum(ElasticTimePeriod creationPeriod)
        {
            var retVal = new HistorischerZeitraum
            {
                Von = new HistorischerZeitpunkt
                    {Ca = creationPeriod.StartDateApproxIndicator, Datum = creationPeriod.StartDate.ToString("yyyy-MM-dd")},
                Bis = new HistorischerZeitpunkt {Ca = creationPeriod.EndDateApproxIndicator, Datum = creationPeriod.EndDate.ToString("yyyy-MM-dd")}
            };
            return retVal;
        }
    }
}