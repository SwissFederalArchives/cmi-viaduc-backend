using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using CMI.Contract.Common.Gebrauchskopie;
using Iiif.API.Presentation;

namespace CMI.Engine.Asset.PostProcess;

public interface IPostProcessManifestCreator
{
    /// <summary>
    ///     Creates the manifest(s) for all documents and dossiers in the DIP package
    /// </summary>
    /// <param name="archiveRecordId"></param>
    /// <param name="paket"></param>
    /// <param name="packageRoot"></param>
    /// <returns>The link to the manifest</returns>
    Uri CreateManifest(string archiveRecordId, PaketDIP paket, string packageRoot);
}

public class PostProcessManifestCreator : IPostProcessManifestCreator
{
    private const string presentationContextUri = "http://iiif.io/api/presentation/3/context.json";
    private readonly ViewerFileLocationSettings locationSettings;
    private readonly IiifManifestSettings manifestSettings;
    private List<PathItem> pathItems = new();
    private string archiveRecordId;
    private List<DateiDIP> packageFiles;
    private List<OrdnerDIP> packageDirectories;
    private PaketDIP paket;

    private string rootDirectory;

    /// <summary>
    /// Just for debugging we can set a flag that will ignore file not found exceptions
    /// </summary>
    public bool IgnoreFileNotFoundExceptions { get; set; }

    public PostProcessManifestCreator(IiifManifestSettings manifestSettings, ViewerFileLocationSettings locationSettings)
    {
        this.manifestSettings = manifestSettings;
        this.locationSettings = locationSettings;
    }

    public Uri CreateManifest(string archiveRecordId, PaketDIP paket, string packageRoot)
    {
        // Store some important variables
        rootDirectory = packageRoot;
        packageFiles = GetAllPackageFiles(paket);
        packageDirectories = GetAllPackageDirectories(paket);
        this.paket = paket;
        this.archiveRecordId = archiveRecordId;
        pathItems = PathHelper.ArchiveIdToPathSegments(archiveRecordId);

        // Get the root dossier from the manifest. This is the entry point
        var dossier = GetRootDossier();

        // Manifest für das Root Dossier
        var id = CreateCollectionManifestForDossier(dossier, null);

        // Verarbeitung von Subdossiers des Root Dossiers
        ProcessDossiers(dossier.Dossier.OrderItems(), id);

        // Verarbeitung von Dokumenten des Root Dossiers
        ProcessDocuments(dossier.Dokument.OrderItems(), id);

        return manifestSettings.PublicManifestWebUri.MakeRelativeUri(id);
    }

    private DossierDIP GetRootDossier()
    {
        foreach (var ordnungssystemposition in paket.Ablieferung.Ordnungssystem.Ordnungssystemposition)
        {
            var dossier = FindRootDossier(ordnungssystemposition);
            if (dossier != null)
            {
                return dossier;
            }
        }

        return null;
    }

    private void ProcessDocuments(List<DokumentDIP> documents, Uri id)
    {
        // Verarbeitung der Dokumente gemäss spezieller Sortierreihenfolge
        foreach (var document in documents.OrderItems())
        {
            CreateCollectionManifestForDokument(document, id);
        }
    }

    private void ProcessDossiers(List<DossierDIP> dossiers, Uri parentItem)
    {
        // Verarbeitung der Dossiers gemäss spezieller Sortierreihenfolge
        foreach (var dossier in dossiers.OrderItems())
        {
            // subdossiers are saved in a path that has the same name as the dossier
            var dirName = GetDirectoryName(dossier.Id);
            if (dirName != null)
            {
                pathItems.Add(dirName);
            }

            var dossierItem = CreateCollectionManifestForDossier(dossier, parentItem);

            // Allfällige Dokumente
            ProcessDocuments(dossier.Dokument.OrderItems(), dossierItem);

            // Weitere Unterdossiers
            ProcessDossiers(dossier.Dossier.OrderItems(), dossierItem);

            // Pop last pathItem
            pathItems.Remove(pathItems.Last());
        }
    }

    /// <summary>
    ///     Creates a collection manifest for a dossier
    /// </summary>
    /// <param name="dossier">The DIP representation of the dossier</param>
    /// <param name="parentManifestId">The id of the parent collection manifest</param>
    /// <returns>The URI of the generated manifest</returns>
    private Uri CreateCollectionManifestForDossier(DossierDIP dossier, Uri parentManifestId)
    {
        var relativePath = string.Join("/", pathItems.Select(p => p.ValidPath));

        // Start des Manifests
        var presentation = new Presentation();
        
        // The top level dossier receives the archiveRecordId as fileName. All other receive the id of the dossier
        var fileName = $"{(parentManifestId == null ? archiveRecordId : IdToValidUrl(dossier.Id))}.json";
        presentation.Context = presentationContextUri;
        presentation.Id = new Uri(manifestSettings.PublicManifestWebUri, $"{relativePath}/{fileName}");
        presentation.Type = "Collection";
        presentation.Label = new LanguageValue
        {
            Invariant = new List<string> { string.IsNullOrEmpty(dossier.Titel) ? "unbekannt" : dossier.Titel }
        };

        // If we have a parent, create the link
        if (parentManifestId != null)
        {
            presentation.PartOf = new List<PartOfElement>
            {
                new() {Id = parentManifestId, Type = "Collection"}
            };
        }

        AddDossierMetadata(dossier, presentation);

        // Add the sub items
        presentation.Items = AddDokumentsAndSubdossiers(dossier, relativePath);

        // Add rendering
        presentation.Rendering = new List<RenderingElement>
            {
                new()
                {
                    Id = new Uri(manifestSettings.PublicDetailRecordUri, $"#/de/archiv/einheit/{archiveRecordId}"),
                    Type = "Text",
                    Label = new LanguageValue
                    {
                        German = new List<string> {"Laden Sie die Unterlagen von der Detailansicht herunter"},
                        French = new List<string> {"Téléchargez les documents depuis l’affichage complet"},
                        Italian = new List<string> {"Scarica i documenti dalla visualizzazione dei dettagli"},
                        Englisch = new List<string> { "Download the documents from the detail view" }
                    },
                    Format = "text/plain"
                }
            };

        var ocrFile = $"{rootDirectory}\\content\\OCR-Text-komplett.zip";
        if (parentManifestId == null && File.Exists(ocrFile))
        {
            presentation.Rendering.Add(new RenderingElement
                {
                    Id = new Uri(manifestSettings.PublicOcrWebUri,
                        $"{relativePath}/OCR-Text-komplett.zip"),
                    Type = "Zip",
                    Label = new LanguageValue
                    {
                        German = new List<string> {"Download OCR"},
                        French = new List<string> {"Download OCR"},
                        Italian = new List<string> {"Download OCR"},
                        Englisch = new List<string> {"Download OCR"}
                    },
                    Format = "application/zip"
                }
            );
        }


        // Save the file
        var currentPath = string.Join("\\", pathItems.Select(p => p.ValidPath));
        var path = Path.Combine(locationSettings.ManifestOutputSaveDirectory, currentPath);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var fullFileName = Path.Combine(path, fileName);
        File.WriteAllText(fullFileName, presentation.ToJson());

        // If a dossier directly has documents, then we need to create a dokument for that dossier as well
        foreach (var dateiRef in dossier.DateiRef)
        {
            // Check if we have a file that not is a premis file
            var dateiName = GetFileName(dateiRef);
            if (dateiName.EndsWith("_PREMIS.xml"))
            {
                continue;
            }

            var dokument = new DokumentDIP
            {
                Titel = dateiName,
                DateiRef = new List<string>{dateiRef},
                Id = dateiRef
            };
            CreateCollectionManifestForDokument(dokument, presentation.Id, true);
        }

        // Return the identifier
        return presentation.Id;
    }

    private void CreateCollectionManifestForDokument(DokumentDIP dokument, Uri parentManifest, bool isFileRefToDossier = false)
    {
        var relativePath = string.Join("/", pathItems.Select(p => p.ValidPath));

        var popPathItem = false;
        var newRelPath = $"{relativePath}";
        var dokumentName = GetFileName(dokument.Id);

        if (string.IsNullOrEmpty(dokumentName))
        {
            dokumentName = PathHelper.CreateShortValidUrlName(Path.GetFileNameWithoutExtension(GetFileName(dokument.DateiRef.First())), true);
            pathItems.Add(GetDirectoryName(dokument.Id));
            // if filename is null, then we do have a document that points to a directory, so we adjust the relative path 
            newRelPath = $"{relativePath}/{GetDirectoryName(dokument.Id)?.ValidPath}";
            popPathItem = true;
        }


        // Start des Manifests
        var presentation = new Presentation();
        var fileName = $"{(parentManifest == null ? archiveRecordId : IdToValidUrl(dokument.Id))}.json";
        presentation.Context = presentationContextUri;
        presentation.Id = new Uri(manifestSettings.PublicManifestWebUri, $"{newRelPath}/{fileName}");
        presentation.Type = "Manifest";
        presentation.Label = new LanguageValue
        {
            Invariant = new List<string> { 
                string.IsNullOrEmpty(dokument.Titel) ? "unbekannt" : dokument.Titel 
            }
        };

        // If we have a parent, create the link
        presentation.PartOf = new List<PartOfElement>
        {
            new() {Id = parentManifest, Type = "Collection"}
        };

        // Add the metadata

        AddDokumentMetadata(dokument, presentation);

        // Search Service
        presentation.Service = new List<ServiceElement>
        {
            new()
            {
                Type = "SearchService1",
                Profile = "http://iiif.io/api/search/1/search",
                // behind the manifest part we need to add the content of the "source" field of the Solr index.
                // As / in the URL are interpreted differently we change them to "-" so the search can function 
                Id = new Uri(manifestSettings.ApiServerUri, $"iiif/search/manifest/{newRelPath.Replace("/", "-")}{(popPathItem || isFileRefToDossier ? "": "-" + dokumentName)}")
            }
        };

        // Add thumbnail
        presentation.Thumbnail = AddThumbnailElement(dokument, newRelPath, isFileRefToDossier);

        // Add rendering
        presentation.Rendering = new List<RenderingElement>
        {
            new()
            {
                Id = new Uri(manifestSettings.PublicDetailRecordUri, $"#/de/archiv/einheit/{archiveRecordId}"),
                Type = "Text",
                Label = new LanguageValue
                {
                    German = new List<string> {"Laden Sie die Unterlagen von der Detailansicht herunter"},
                    French = new List<string> {"Téléchargez les documents depuis l’affichage complet"},
                    Italian = new List<string> {"Scarica i documenti dalla visualizzazione dei dettagli"},
                    Englisch = new List<string> { "Download the documents from the detail view" }
                },
                Format = "text/plain"
            }
        };

        var parts = PathHelper.ArchiveIdToPathSegments(archiveRecordId);
        var pathParts = string.Join("\\", pathItems.Skip(parts.Count).Select(p => p.PhysicalPath));

        var ocrFile = string.Empty;
        if (string.IsNullOrEmpty(pathParts))
        {
            ocrFile = !isFileRefToDossier ?
                $"{rootDirectory}\\content\\{GetDirectoryName(dokument.Id)?.PhysicalPath}_OCR.txt" :
                $"{rootDirectory}\\content\\content_OCR.txt";
        }
        else
        {
            ocrFile = !isFileRefToDossier ?
                $"{rootDirectory}\\content\\{pathParts}\\{GetDirectoryName(dokument.Id)?.PhysicalPath}_OCR.txt" :
                $"{rootDirectory}\\content\\{pathParts}\\content_OCR.txt";
        }

        if (File.Exists(ocrFile))
        {
            var id = !isFileRefToDossier
                ? new Uri(manifestSettings.PublicOcrWebUri,
                    $"{newRelPath}/{GetDirectoryName(dokument.Id)?.ValidPath}_OCR.txt")
                : new Uri(manifestSettings.PublicOcrWebUri,
                    Path.ChangeExtension($"{newRelPath}/{dokumentName})",".txt"));
            
            presentation.Rendering.Add(new RenderingElement
            {
                Id = id,
                Type = "Text",
                Label = new LanguageValue
                {
                    German = new List<string> { "Download OCR" },
                    French = new List<string> { "Download OCR" },
                    Italian = new List<string> { "Download OCR" },
                    Englisch = new List<string> { "Download OCR" }
                },
                Format = "text/plain"
            }
            );
        }

        // Add the sub items
        presentation.Items = AddDokumentPages(dokument, newRelPath, isFileRefToDossier);

        // Save the file
        var currentPath = string.Join("\\", pathItems.Select(p => p.ValidPath));
        var path = Path.Combine(locationSettings.ManifestOutputSaveDirectory, currentPath);
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        var fullFileName = Path.Combine(path, fileName);
        File.WriteAllText(fullFileName, presentation.ToJson());

        if (popPathItem)
        {
            pathItems.Remove(pathItems.Last());
        }
    }
    private List<IiifItem> AddDokumentPages(DokumentDIP dokument, string relativeUri, bool isFileRefToDossier)
    {
        var retVal = new List<IiifItem>();
        var pageNo = 0;
        foreach (var dateiRef in dokument.DateiRef)
        {
            var dateiLocation = FindFileInPackage(dateiRef, paket.Inhaltsverzeichnis.Ordner);
            if (dateiLocation != null && !dateiLocation.FullName.EndsWith("_PREMIS.xml"))
            {
                pageNo++;
                var dimensions = GetImageDimensions(dateiLocation);

                var fi = new FileInfo(dateiLocation.FullName);
                string fileName;
                LanguageValue label;
                switch (fi.Extension.ToLower())
                {
                    case ".jp2":
                    case ".tif":
                    case ".tiff":
                        fileName = Path.ChangeExtension(fi.Name, ".jpg");
                        label = new LanguageValue
                        {
                            Invariant = new List<string> { $"{pageNo}" },
                        };
                        break;
                    default:
                        fileName = fi.Name;
                        label = new LanguageValue {Invariant = new List<string> {fi.Name}};
                        break;
                }

                fileName = PathHelper.CreateShortValidUrlName(fileName, true);
                var dokumentFileName = isFileRefToDossier ? "" : GetFileName(dokument.Id);

                retVal.Add(new IiifItem
                {
                    Id = new Uri(manifestSettings.ImageServerUri, $"iiif/2/{relativeUri}/{(string.IsNullOrEmpty(dokumentFileName) ? "" : dokumentFileName + "/")}{fileName}"),
                    Type = "Canvas",
                    Label = label,
                    Width = dimensions.Width,
                    Height = dimensions.Height,
                    Items = new List<AnnotationPage>
                    {
                        new()
                        {
                            Context = new Uri("http://iiif.io/api/presentation/3/context.json"),
                            // Id according to "convention". Does not point anywhere
                            Id = new Uri(manifestSettings.PublicManifestWebUri,
                                $"{relativeUri}/{IdToValidUrl(dokument.Id)}.json#annotations-page-painting-{IdToValidUrl(dokument.Id)}-{pageNo}"),
                            Type = "AnnotationPage",
                            Items = new List<Annotation>
                            {
                                new()
                                {
                                    Id = new Uri(manifestSettings.PublicManifestWebUri,
                                        $"{relativeUri}/{IdToValidUrl(dokument.Id)}.json#annotations-content-painting-{IdToValidUrl(dokument.Id)}-{pageNo}"),
                                    Type = "Annotation",
                                    Motivation = "painting",
                                    Target = new Uri(manifestSettings.PublicManifestWebUri, $"{relativeUri}/canvasses/{IdToValidUrl(dokument.Id)}-{pageNo}"),
                                    Body = CreateBody(dokument, relativeUri, isFileRefToDossier, fileName, dimensions)
                                }
                            }
                        }
                    }
                });
            }
        }

        return retVal;
    }

    private BodyClass CreateBody(DokumentDIP dokument, string relativeUri, bool isFileRefToDossier, string fileName, Size dimensions)
    {
        var fi = new FileInfo(fileName);
        switch (fi.Extension.ToLower())
        {
            case ".jpg":
                return CreateImageBody(dokument, relativeUri, isFileRefToDossier, fileName, dimensions);
            default:
                return CreateDokumentBody(dokument, relativeUri, isFileRefToDossier, fileName);
        }
    }

    private BodyClass CreateImageBody(DokumentDIP dokument, string relativeUri, bool isFileRefToDossier, string fileName, Size dimensions)
    {
        return new BodyClass
        {
            Id = new Uri(manifestSettings.ImageServerUri,
                $"iiif/2/{Uri.EscapeDataString($"{relativeUri}/{(isFileRefToDossier ? "" : GetFileName(dokument.Id) + "/")}{fileName}")}/full/full/0/default.jpg"),
            Type = "Image",
            Format = "image/jpeg",
            Width = dimensions.Width,
            Height = dimensions.Height,
            Service = new List<ServiceElement>
            {
                new()
                {
                    Id = new Uri(manifestSettings.ImageServerUri,
                        $"iiif/2/{Uri.EscapeDataString($"{relativeUri}/{(isFileRefToDossier ? "" : GetFileName(dokument.Id) + "/")}{fileName}")}"),
                    Type = "ImageService2",
                    Profile = "level2"
                }
            }
        };
    }

    private BodyClass CreateDokumentBody(DokumentDIP dokument, string relativeUri, bool isFileRefToDossier, string fileName)
    {
        return new BodyClass
        {
            Id = new Uri(manifestSettings.PublicContentWebUri,
                $"{relativeUri}/{(isFileRefToDossier ? "" : GetFileName(dokument.Id))}/{fileName}"),
            Type = "foaf:Document",
            Format = GetMimeMapping(fileName)
        };
    }

    private static void AddDossierMetadata(DossierDIP dossier, Presentation presentation)
    {
        #region Add Metadata

        // Get the data
        var signatur = dossier.zusatzDaten.GetZusatzMerkmal("Signatur");
        var entstehungszeitraum = dossier.zusatzDaten.GetZusatzMerkmal("Entstehungszeitraum Anzeigetext");
        if (string.IsNullOrEmpty(entstehungszeitraum))
        {
            entstehungszeitraum = $"{dossier.Entstehungszeitraum.Von.Datum} - {dossier.Entstehungszeitraum.Bis.Datum}";
        }

        var land = dossier.zusatzDaten.GetZusatzMerkmal("Land");
        var darin = dossier.Inhalt;


        // Add the metadata
        presentation.Metadata = new List<RequiredStatementElement>();
        if (!string.IsNullOrEmpty(signatur))
        {
            presentation.Metadata.Add(AddRequiredStatementElement("Signatur", signatur, "Signatur", "Cote", "Segnatura", "Reference code"));
        }

        if (!string.IsNullOrEmpty(dossier.Titel))
        {
            presentation.Metadata.Add(AddRequiredStatementElement("Titel", dossier.Titel, "Titel", "Titre", "Titolo", "Title"));
        }

        if (!string.IsNullOrEmpty(entstehungszeitraum))
        {
            presentation.Metadata.Add(AddRequiredStatementElement("Entstehungszeitraum", entstehungszeitraum, "Entstehungszeitraum",
                "Date de création", "Periodo di costituzione", "Creation period"));
        }

        if (!string.IsNullOrEmpty(dossier.Aktenzeichen))
        {
            presentation.Metadata.Add(AddRequiredStatementElement("Aktenzeichen", dossier.Aktenzeichen, "Aktenzeichen", "Référence", "Riferimento",
                "File reference"));
        }

        if (!string.IsNullOrEmpty(land))
        {
            presentation.Metadata.Add(AddRequiredStatementElement("Land", land, "Land", "Pays", "Paese", "Country"));
        }

        if (!string.IsNullOrEmpty(darin))
        {
            presentation.Metadata.Add(AddRequiredStatementElement("Darin", darin, "Darin", "Contenu", "Contiene", "Contains"));
        }

        #endregion
    }

    private void AddDokumentMetadata(DokumentDIP dokument, Presentation presentation)
    {
        // Get the data
        var title = dokument.Titel;

        var signatur = dokument.zusatzDaten.GetZusatzMerkmal("Signatur");
        var entstehungszeitraum = dokument.zusatzDaten.GetZusatzMerkmal("Entstehungszeitraum Anzeigetext");
        if (string.IsNullOrEmpty(entstehungszeitraum))
        {
            entstehungszeitraum = $"{dokument.Entstehungszeitraum.Von.Datum} - {dokument.Entstehungszeitraum.Bis.Datum}".Trim();
            if (entstehungszeitraum == "-")
            {
                entstehungszeitraum = null;
            }
        }

        var darin = dokument.zusatzDaten.GetZusatzMerkmal("Darin");
        presentation.Metadata = new List<RequiredStatementElement>();
        if (!string.IsNullOrEmpty(signatur))
        {
            presentation.Metadata.Add(AddRequiredStatementElement("Signatur", signatur, "Signatur", "Cote", "Segnatura", "Reference code"));
        }

        if (!string.IsNullOrEmpty(dokument.Titel))
        {
            
            presentation.Metadata.Add(AddRequiredStatementElement("Titel", title, "Titel", "Titre", "Titolo", "Title"));
        }

        if (!string.IsNullOrEmpty(entstehungszeitraum))
        {
            presentation.Metadata.Add(AddRequiredStatementElement("Entstehungszeitraum", entstehungszeitraum, "Entstehungszeitraum",
                "Date de création", "Periodo di costituzione", "Creation period"));
        }

        if (!string.IsNullOrEmpty(darin))
        {
            presentation.Metadata.Add(AddRequiredStatementElement("Darin", darin, "Darin", "Contenu", "Contiene", "Contains"));
        }
    }

    private string GetMimeMapping(string fileName)
    {
        var fi = new FileInfo(fileName);
        switch (fi.Extension.ToLower())
        {
            case ".pdf":
                return "application/pdf";
            case ".txt":
                return "text/plain";
            case ".xml":
                return "application/xml";
            case ".json":
                return "application/json";
            default:
                return "application/octet-stream";
        }
    }

    private Size GetImageDimensions(PackageFileLocation dateiLocation)
    {
        var retVal = new Size(1, 1);
        var imagePath = Path.Combine(rootDirectory, dateiLocation.FullName);

        if (imagePath.EndsWith(".jp2", StringComparison.InvariantCultureIgnoreCase)
            || imagePath.EndsWith(".tif", StringComparison.InvariantCultureIgnoreCase)
            || imagePath.EndsWith(".tiff", StringComparison.InvariantCultureIgnoreCase))
        {
            imagePath = Path.ChangeExtension(imagePath, ".jpg");
        }

        // Only if we have images, then get the real size
        // For pdf and others return default Size of 1x1
        if (imagePath.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase))
        {
            if (!File.Exists(imagePath))
            {
                if (!IgnoreFileNotFoundExceptions)
                {
                    throw new FileNotFoundException("Unable to find image file", imagePath);
                }

                retVal.Height = 0;
                retVal.Width = 0;
                return retVal;
            }

            using var fileStream = new FileStream(imagePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var image = Image.FromStream(fileStream, false, false);
            retVal.Height = image.Height;
            retVal.Width = image.Width;
        }

        return retVal;
    }

    private List<IiifItem> AddDokumentsAndSubdossiers(DossierDIP dossier, string relativePath)
    {
        var retVal = new List<IiifItem>();

        // Get the ordered items that can either be "dossiers" or "documents"
        var orderedItems = GetOrderedItems(dossier);

        foreach (var orderedItem in orderedItems.OrderBy(i => i.SortText))
        {
            // Try to get a dossier
            var dossierDip = dossier.Dossier.Find(d => d.Id == orderedItem.Id);
            if (dossierDip != null)
            {
                retVal.Add(GetCollectionReference(relativePath, dossierDip));
                continue;
            }

            // If no dossier was found, try to get the document
            var dokumentDip = dossier.Dokument.Find(d => d.Id == orderedItem.Id);
            if (dokumentDip != null)
            {
                retVal.Add(GetManifestReference(relativePath, dokumentDip));
            }
        }

        // if a dossier directly has file refs, it is mostly the premis file
        // but can be individual files
        foreach (var dateiRef in dossier.DateiRef)
        {
            // Check if we have a file that not is a premis file
            var fileName = GetFileName(dateiRef);
            if (fileName.EndsWith("_PREMIS.xml"))
            {
                continue;
            }

            var fileNamePart = PathHelper.CreateShortValidUrlName(Path.GetFileNameWithoutExtension(GetFileName(dateiRef)), true);

            retVal.Add(new IiifItem
            {
                Id = new Uri(manifestSettings.PublicManifestWebUri, $"{relativePath}/{dateiRef}.json"),
                Type = "Manifest",
                Label = new LanguageValue { Invariant = new List<string> { fileNamePart } }
            });

            // Creating ad-hoc element for getting the thumbnail
            var dokumentDip = new DokumentDIP
            {
                Titel = dateiRef,
                DateiRef = new List<string> { dateiRef }
            };

            // Add thumbnail(s)
            retVal.Last().Thumbnail = AddThumbnailElement(dokumentDip, relativePath, true);
        }

        return retVal;
    }


    /// <summary>
    /// Returns the reference to the manifest for a dokument
    /// </summary>
    private IiifItem GetManifestReference(string relativePath, DokumentDIP dokumentDip)
    {
        var fileName = GetFileName(dokumentDip.Id);

        // if filename is null, then we do have a document that points to a directory 
        var newRelPath = $"{relativePath}/{(string.IsNullOrEmpty(fileName) ? $"{GetDirectoryName(dokumentDip.Id)?.ValidPath}/" : "")}";

        var item = new IiifItem
        {
            Id = new Uri(manifestSettings.PublicManifestWebUri, $"{newRelPath}{IdToValidUrl(dokumentDip.Id)}.json"),
            Type = "Manifest",
            Label = new LanguageValue { Invariant = new List<string> { dokumentDip.Titel } },
            Thumbnail = AddThumbnailElement(dokumentDip, newRelPath)
        };

        return item;
    }

    /// <summary>
    /// Returns the reference to the collection manifest for a dossier
    /// </summary>
    private IiifItem GetCollectionReference(string relativePath, DossierDIP dossierDip)
    {
        var dirName = GetDirectoryName(dossierDip.Id);
        var newRelPath = $"{relativePath}/{(dirName == null ? "" : $"{dirName.ValidPath}/")}";
        return new IiifItem
        {
            Id = new Uri(manifestSettings.PublicManifestWebUri, $"{newRelPath}{IdToValidUrl(dossierDip.Id)}.json"),
            Type = "Collection",
            Label = new LanguageValue {Invariant = new List<string> {dossierDip.Titel}}
        };
    }

    private static List<OrderedDipItem> GetOrderedItems(DossierDIP dossier)
    {
        // Use a temporary collection to sort dossiers and dokuments at the same time
        var orderedItems = dossier.Dossier.Select(d => new OrderedDipItem
        {
            Id = d.Id,
            SortText = d.zusatzDaten.GetZusatzMerkmal("ReihenfolgeAnalogesDossier")
        }).ToList();
        orderedItems.AddRange(dossier.Dokument.Select(d => new OrderedDipItem
        {
            Id = d.Id,
            SortText = d.zusatzDaten.GetZusatzMerkmal("ReihenfolgeAnalogesDossier")
        }).ToList());
        return orderedItems;
    }

    private List<ThumbnailElement> AddThumbnailElement(DokumentDIP dokument, string relativePath, bool isFileRefToDossier = false)
    {
        // Add thumbnail
        var fileRef = dokument.DateiRef.FirstOrDefault();
        if (fileRef != null)
        {
            var location = FindFileInPackage(fileRef, paket.Inhaltsverzeichnis.Ordner);
            var fi = new FileInfo(location.Datei.Name);
            string thumbnailName;
            switch (fi.Extension.ToLower())
            {
                case ".jp2":
                case ".tif":
                case ".tiff":
                    thumbnailName = $"{Path.ChangeExtension(fi.Name, ".jpg")}";
                    thumbnailName = PathHelper.CreateShortValidUrlName(thumbnailName, true);

                    var newRelativePath = !isFileRefToDossier
                        ? $"{Uri.EscapeDataString($"{relativePath}/{GetFileName(dokument.Id)}/{thumbnailName}")}/full/150,/0/default.jpg"
                        : $"{Uri.EscapeDataString($"{relativePath}/{thumbnailName}")}/full/150,/0/default.jpg";

                    return new List<ThumbnailElement>
                    {
                        new()
                        {
                            Id = new Uri(manifestSettings.ImageServerUri, $"iiif/2/{newRelativePath}"),
                            Format = "image/jpeg",
                            Type = "Image"
                        }
                    };
                case ".pdf":
                    thumbnailName = "pdfThumbnail.svg";
                    break;
                default:
                    thumbnailName = "defaultThumbnail.svg";
                    break;
            }

            return new List<ThumbnailElement>
            {
                new()
                {
                    Id = new Uri(manifestSettings.PublicContentWebUri, $"_icons/{thumbnailName}"),
                    Format = "image/svg+xml"
                }
            };
        }

        return new List<ThumbnailElement>();
    }

    /// <summary>
    ///     Adds a label value pair. If label contains a value the label is added as language invariant. Else you must provide
    ///     a value for each language.
    ///     Same with the value. If value contains a value, then this is used as language invariant. Else you need to provide a
    ///     value for each language.
    /// </summary>
    /// <param name="label"></param>
    /// <param name="labelDe"></param>
    /// <param name="labelFr"></param>
    /// <param name="labelIt"></param>
    /// <param name="labelEn"></param>
    /// <param name="value"></param>
    /// <param name="valueDe"></param>
    /// <param name="valueFr"></param>
    /// <param name="valueIt"></param>
    /// <param name="valueEn"></param>
    /// <returns></returns>
    private static RequiredStatementElement AddRequiredStatementElement(string label, string value,
        string labelDe = null, string labelFr = null, string labelIt = null, string labelEn = null,
        string valueDe = null, string valueFr = null, string valueIt = null, string valueEn = null)
    {
        var retVal = new RequiredStatementElement() {Label = new LanguageValue(), Value = new LanguageValue()};

        if (!string.IsNullOrEmpty(label))
        {
            retVal.Label.Invariant = new List<string> {label};
            retVal.Value.Invariant = new List<string> { value }; ;
        }

        if (!string.IsNullOrEmpty(labelDe))
        {
            retVal.Label.German = new List<string> { labelDe };
            retVal.Value.German = new List<string> {valueDe ?? value}; ;
        }

        if (!string.IsNullOrEmpty(labelEn))
        {
            retVal.Label.Englisch = new List<string> { labelEn };
            retVal.Value.Englisch = new List<string> { valueEn ?? value }; ;
        }

        if (!string.IsNullOrEmpty(labelFr))
        {
            retVal.Label.French = new List<string> { labelFr };
            retVal.Value.French = new List<string> { valueFr ?? value }; ;
        }

        if (!string.IsNullOrEmpty(labelIt))
        {
            retVal.Label.Italian = new List<string> { labelIt };
            retVal.Value.Italian = new List<string> { valueIt ?? value }; ;
        }

        return retVal;
    }

    private DossierDIP FindRootDossier(OrdnungssystempositionDIP ordnungssystemposition)
    {
        if (ordnungssystemposition.Dossier.Any())
        {
            return ordnungssystemposition.Dossier.First();
        }

        // Process any sub items
        foreach (var ordnungssystemSubPosition in ordnungssystemposition.Ordnungssystemposition)
        {
            var dossier = FindRootDossier(ordnungssystemSubPosition);
            if (dossier != null)
            {
                return dossier;
            }
        }

        return null;
    }

    private string IdToValidUrl(string dokumentOrDossierId)
    {
        return PathHelper.CreateShortValidUrlName(dokumentOrDossierId, false);
    }

    private string GetFileName(string dateiRef)
    {
        var fileName = packageFiles.Find(f => f.Id == dateiRef)?.Name;
        return PathHelper.CreateShortValidUrlName(fileName, true);
    }

    private PathItem GetDirectoryName(string directoryRef)
    {
        // Unsere eigenen Metadata.xml haben beim Dossier (Ordner) noch ein "_D" angehängt
        // Ein "logisches" Dossier hat keinen Eintrag im Inhaltsverzeichnis, deshalb kann es nicht gefunden werden.
        // In diesem Fall liefern wir null zurück
        var hit = packageDirectories.Find(f => f.Id == directoryRef || f.Id == directoryRef + "_D");
        if (hit != null)
        {
            return new PathItem(hit.Name, PathHelper.CreateShortValidUrlName(hit.Name, false));
        }

        return new PathItem(string.Empty, string.Empty);
    }

    private static List<DateiDIP> GetAllPackageFiles(PaketDIP paket)
    {
        var files = paket.Inhaltsverzeichnis.Datei;
        var allFolders = paket.Inhaltsverzeichnis.Ordner.SelectManyAllInclusive(o => o.Ordner);
        files.AddRange(allFolders.SelectMany(f => f.Datei));
        return files;
    }

    private static List<OrdnerDIP> GetAllPackageDirectories(PaketDIP paket)
    {
        var allFolders = paket.Inhaltsverzeichnis.Ordner.SelectManyAllInclusive(o => o.Ordner);
        return allFolders.ToList();
    }


    private static PackageFileLocation FindFileInPackage(string dateiRef, List<OrdnerDIP> ordnerList, PackageFileLocation retVal = null,
        OrdnerDIP parentOrdner = null)
    {
        retVal ??= new PackageFileLocation();

        if (parentOrdner != null)
        {
            retVal.OrdnerList.Add(parentOrdner);
        }

        if (!ordnerList.Any())
        {
            return retVal;
        }

        foreach (var ordner in ordnerList)
        {
            foreach (var datei in ordner.Datei)
            {
                if (datei.Id == dateiRef)
                {
                    retVal.OrdnerList.Add(ordner);
                    retVal.Datei = datei;
                    return retVal;
                }
            }

            var dateiSub = FindFileInPackage(dateiRef, ordner.Ordner, retVal, ordner);
            if (dateiSub is { Datei: { } })
            {
                return dateiSub;
            }
            else
            {
                retVal.OrdnerList.Remove(retVal.OrdnerList.Last());
            }
        }

        return null;
    }

    private class OrderedDipItem
    {
        public string Id { get; set; }
        public string SortText { get; set; }
    }
}