using System;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Autofac.Features.Indexed;
using CMI.Utilities.Common.Providers;
using CMI.Web.Common.Helpers;
using Serilog;


namespace CMI.Web.Frontend.Controllers;

public class FilesController : BaseController
{
    private readonly IStorageProvider fileProvider;
    private readonly IStorageProvider configuredProvider;
    private StorageProviders storageProvider;

    public FilesController(IIndex<StorageProviders, IStorageProvider> storageProviders, StorageProviders storageProvider)
    {
        this.storageProvider = storageProvider;
        if (!storageProviders.TryGetValue(StorageProviders.File, out fileProvider))
        {
            Log.Error("No File Provider");
        }
        if (!storageProviders.TryGetValue(StorageProviders.S3, out IStorageProvider s3Provider))
        {
            Log.Error("S3 Provider not available");
        }
        configuredProvider = storageProvider == StorageProviders.File ? fileProvider : s3Provider;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult> Index(string subdirectory, string relativeFile)
    {
        var contentType = MimeMapping.GetMimeMapping(relativeFile);
        MemoryStream response;
        switch (contentType)
        {
            case "application/pdf":
                if (storageProvider == StorageProviders.File)
                {
                    response = await configuredProvider.ReadFileAsync(new Uri(Path.Combine(WebHelper.ViewerFileLocationBaseDirectory, subdirectory, relativeFile), UriKind.Absolute));
                }
                else
                {
                    response = await configuredProvider.ReadFileAsync(new Uri(subdirectory + "/" + relativeFile, UriKind.Relative));
                }
                break;
            default:
                response = await fileProvider.ReadFileAsync(new Uri(Path.Combine(WebHelper.ViewerFileLocationBaseDirectory, subdirectory , relativeFile), UriKind.Absolute));
                break;
        }
   
        if (response == null)
        {
            return HttpNotFound("Die Datei wurde nicht gefunden");
        }
        response.Position = 0;
        return File(response, contentType, relativeFile);
    }
}