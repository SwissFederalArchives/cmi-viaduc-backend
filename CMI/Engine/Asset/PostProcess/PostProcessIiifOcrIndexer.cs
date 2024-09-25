using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CMI.Contract.Common;
using CMI.Contract.Common.Gebrauchskopie;
using CMI.Engine.Asset.Solr;
using CMI.Utilities.Common.Helpers;
using CommonServiceLocator;
using Serilog;
using SolrNet;

namespace CMI.Engine.Asset.PostProcess;

public class PostProcessIiifOcrIndexer : ProcessAnalyzerBase
{
    private readonly SolrConnectionInfo solrConnectionInfo;
    private readonly IiifManifestSettings manifestSettings;
    private AddParameters addParameters;
    private ISolrOperations<SolrRecord> solr;
    private List<DateiDIP> packageFiles;
    private List<OrdnerDIP> packageDirectories;

    public string RootFolder { get; set; }
    public string ArchiveRecordId { get; set; }
    public PaketDIP Paket { get; set; }

    public PostProcessIiifOcrIndexer(SolrConnectionInfo solrConnectionInfo, IiifManifestSettings manifestSettings)
    {
        this.solrConnectionInfo = solrConnectionInfo;
        this.manifestSettings = manifestSettings;
    }

    public override void AnalyzeRepositoryPackage(RepositoryPackage package, string rootFolder)
    {
        if (!solrConnectionInfo.SolrUrl.Equals("SkipSolrForTesting"))
        {
            InitializeSolr();
        }

        Debug.Assert(Paket != null, "Paket property must be set before calling AnalyzeRepositoryPackage");
        packageFiles = GetAllPackageFiles(Paket);
        packageDirectories = GetAllPackageDirectories(Paket);

        base.AnalyzeRepositoryPackage(package, rootFolder);
    }


    protected override void AnalyzeFiles(string rootOrSubFolder, List<RepositoryFile> files)
    {
        CopyOcrFiles(rootOrSubFolder);
        IndexOcrFiles(rootOrSubFolder, files);
    }

    private void InitializeSolr()
    {
        Startup.InitContainer();
        Startup.Init<SolrRecord>(solrConnectionInfo.SolrUrl + solrConnectionInfo.SolrCoreName);
        solr = ServiceLocator.Current.GetInstance<ISolrOperations<SolrRecord>>();
        if (!Directory.Exists(solrConnectionInfo.SolrHighlightingPath))
        {
            Directory.CreateDirectory(solrConnectionInfo.SolrHighlightingPath );
        }

        addParameters = new AddParameters
        {
            CommitWithin = 1000,
            Overwrite = true
        };
    }

    private void CopyOcrFiles(string rootOrSubFolder)
    {
        var directory = new DirectoryInfo(rootOrSubFolder);
        if (directory.Exists)
        {
            var files = directory.GetFiles();
            foreach (var file in files.Where(f => f.Extension.EndsWith(".hOcr", StringComparison.InvariantCultureIgnoreCase)))
            {
                if (file.Exists)
                {
                    var hOcrDirectory = GetHOcrDestinationDirectory(rootOrSubFolder);

                    var shortenedFileName = PathHelper.CreateShortValidUrlName(file.Name, true);
                    var fi = new FileInfo(hOcrDirectory.FullName + @"\" + shortenedFileName);
                    if (fi.Exists)
                    {
                        try
                        {
                            fi.Delete();
                        }
                        catch (Exception)
                        {
                            // As solr seems to lock the files, we can't delete it
                            // We assume that the document won't change between runs
                            Log.Warning("We have an existing file that seems to be used by another process. Skipping this file {fileName}.", fi.Name);
                            continue;
                        }
                    }
                    File.Copy(file.FullName, fi.FullName);
                }
                else
                {
                    Log.Warning("hOcr-File does not exist: {FullName}", file.FullName);
                }
            }
        }
    }


    private void IndexOcrFiles(string tempFolder, List<RepositoryFile> repositoryFiles)
    {
        var directory = new DirectoryInfo(tempFolder);
        if (directory.Exists)
        {
            var documents = packageFiles.Where(p => !p.Name.EndsWith(".xml") &&
                                                    repositoryFiles.Select(r => r.PhysicalName).Contains(p.Name));
            foreach (var file2 in documents)
            {
                var sourceFile = new FileInfo(Path.Combine(tempFolder, file2.Name));
                var hOcrFile = new FileInfo(Path.ChangeExtension(sourceFile.FullName, ".hOCR"));

                if (hOcrFile.Exists)
                {
                    var dateiLocation = FindFileInPackage(file2.Id, packageDirectories);
                    var hOcrDirectory = GetHOcrDestinationDirectory(tempFolder);
                    var ocrFilePath = hOcrDirectory.FullName.Replace("\\", @"/");
                    var relativePath = tempFolder == RootFolder ? "" : tempFolder.Substring(RootFolder.Length + 1);
                    relativePath = PathHelper.CreateShortValidUrlName(relativePath, false).Replace("\\", @"/");

                    // The source identifies correctly ONE document
                    var parts = PathHelper.ArchiveIdToPathSegments(ArchiveRecordId);
                    var source = $"{string.Join("/", parts.Select(p => p.ValidPath))}/{(string.IsNullOrEmpty(relativePath) ? "" : $"{relativePath}/")}";
                    if (source.EndsWith("/"))
                    {
                        source = source.Substring(0, source.Length - 1);
                    }

                    // The path is the URI of the manifest in which the document is contained
                    var manifestPath = GetManifestPath(source, dateiLocation);

                    if (!UploadSolr(new SolrRecord
                    {
                        Id = $"{source}/{PathHelper.CreateShortValidUrlName(Path.GetFileNameWithoutExtension(hOcrFile.Name), false)}",
                        ArchiveRecordId = ArchiveRecordId,
                        // The source field must match the definition in the SearchService id of the manifest
                        // It cannot (or better should/not) contain slashes as it must be passed as an argument to an existing service
                        Source = Uri.EscapeUriString(source.Replace("/", "-").Replace("\\", "-")),
                        Title = hOcrFile.Name,
                        ImageUrl = Uri.EscapeUriString($"{source}/{Path.ChangeExtension(PathHelper.CreateShortValidUrlName(hOcrFile.Name, true), ".jpg")}"),
                        OCRText = @$"{ocrFilePath}/{PathHelper.CreateShortValidUrlName(hOcrFile.Name, true)}",
                        ManifestPath =  Uri.EscapeUriString(manifestPath.ToString()),
                        ManifestLabel = GetLabel(source)
                    }))
                    {
                        Log.Warning("Solr update not successful for file: {FullName}", hOcrFile.FullName);
                    }
                }
                else
                {
                    Log.Warning("File does not exists: {FullName}", hOcrFile.FullName);
                }
            }
        }
    }

    private DirectoryInfo GetHOcrDestinationDirectory(string sourceDirectory)
    {
        var parts = PathHelper.ArchiveIdToPathSegments(ArchiveRecordId);
        var hOcrDirectory = new DirectoryInfo(solrConnectionInfo.SolrHighlightingPath + @$"\{string.Join("\\", parts.Select(p => p.ValidPath))}");
        var relativePath = sourceDirectory == RootFolder ? "" : sourceDirectory.Substring(RootFolder.Length + 1);

        var newPath = PathHelper.CreateShortValidUrlName(relativePath, false);

        var destinationDirectory = new DirectoryInfo(Path.Combine(hOcrDirectory.FullName, newPath));

        if (!destinationDirectory.Exists)
        {
            destinationDirectory.Create();
        }

        return destinationDirectory;
    }

    private string GetLabel(string path)
    {
        return path?.Substring(path.LastIndexOf('/') + 1) ?? string.Empty;
    }

    private Uri GetManifestPath(string source, PackageFileLocation dateiLocation)
    {
        var manifestFileName = dateiLocation.OrdnerList.LastOrDefault()?.Id;
        if (!string.IsNullOrEmpty(manifestFileName) && manifestFileName.EndsWith("_D"))
        {
            manifestFileName = manifestFileName.Substring(0, manifestFileName.Length - 2);
        }

        manifestFileName += ".json";
        var relativeName = source.EndsWith("/") ? source + manifestFileName : $"{source}/{manifestFileName}";
        return new Uri(manifestSettings.PublicManifestWebUri, relativeName);
    }

    private bool UploadSolr(SolrRecord document)
    {
        if (solrConnectionInfo.SolrUrl.Equals("SkipSolrForTesting"))
        {
            return true;
        }

        var result = solr.Add(document, addParameters);
        if (result.Status == 0)
        {
            var commitResult = solr.Commit();
            if (commitResult.Status == 0)
            {
                return true;
            }
        }

        return false;
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


}