using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CMI.Contract.Common;
using CMI.Engine.Asset.Solr;
using CommonServiceLocator;
using Serilog;
using SolrNet;

namespace CMI.Engine.Asset.PostProcess;

public class PostProcessIiifOcrIndexer : ProcessAnalyzerBase
{
    private readonly SolrConnectionInfo solrConnectionInfo;
    private AddParameters addParameters;
    private ISolrOperations<SolrRecord> solr;

    public string RootFolder { get; set; }
    public string ArchiveRecordId { get; set; }

    public PostProcessIiifOcrIndexer(SolrConnectionInfo solrConnectionInfo)
    {
        this.solrConnectionInfo = solrConnectionInfo;
    }

    public override void AnalyzeRepositoryPackage(RepositoryPackage package, string rootFolder)
    {
        if (!solrConnectionInfo.SolrUrl.Equals("SkipSolrForTesting"))
        {
            InitializeSolr();
        }
        base.AnalyzeRepositoryPackage(package, rootFolder);
    }


    protected override void AnalyzeFiles(string rootOrSubFolder, List<RepositoryFile> files)
    {
        CopyOcrFiles(rootOrSubFolder);
        IndexOcrFiles(rootOrSubFolder);
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


    private void IndexOcrFiles(string tempFolder)
    {
        var directory = new DirectoryInfo(tempFolder);
        if (directory.Exists)
        {
            var files = directory.GetFiles("*.hOcr");
            foreach (var file in files)
            {
                if (file.Exists)
                {
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

                    if (!UploadSolr(new SolrRecord
                        {
                            Id = $"{source}/{PathHelper.CreateShortValidUrlName(Path.GetFileNameWithoutExtension(file.Name), false)}",
                            ArchiveRecordId = ArchiveRecordId,
                            // The source field must match the definition in the SearchService id of the manifest
                            // It cannot (or better should/not) contain slashes as it must be passed as an argument to an existing service
                            Source = Uri.EscapeUriString(source.Replace("/", "-").Replace("\\", "-")),
                            Title = file.Name,
                            ImageUrl = Uri.EscapeUriString($"{source}/{Path.ChangeExtension(PathHelper.CreateShortValidUrlName(file.Name, true), ".jpg")}"),
                            OCRText = @$"{ocrFilePath}/{PathHelper.CreateShortValidUrlName(file.Name, true)}"
                        }))
                    {
                        Log.Warning("Solr update not successful for file: {FullName}", file.FullName);
                    }
                }
                else
                {
                    Log.Warning("File does not exists: {FullName}", file.FullName);
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
}