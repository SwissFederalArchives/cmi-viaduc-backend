using System;
using System.IO;
using System.Threading.Tasks;
using Autofac.Features.Indexed;
using CMI.Engine.Asset.PostProcess;
using CMI.Engine.Asset.Solr;
using CMI.Utilities.Common.Providers;
using Serilog;

namespace CMI.Engine.Asset
{
    public interface IManifestHelper
    {
        /// <summary>
        /// Deletes all IIIF related files for a given manifest link from disk and solr
        /// </summary>
        /// <param name="manifestLink"></param>
        void DeleteIiifFiles(string manifestLink);
    }

    public class ManifestHelper : IManifestHelper
    {
        private readonly ViewerFileLocationSettings locationSettings;
        private readonly IStorageProvider fileProvider;
        private readonly IStorageProvider configuredProvider;
        private readonly ISolrEngine solrEngine;

        public ManifestHelper(ViewerFileLocationSettings locationSettings, ISolrEngine solrEngine,
            IIndex<StorageProviders, IStorageProvider> storageProviders, StorageProviders storageProvider) 
        {
            this.locationSettings = locationSettings;
            if (!storageProviders.TryGetValue(StorageProviders.File, out fileProvider))
            {
                Log.Error("No File Provider");
            }
            if (!storageProviders.TryGetValue(StorageProviders.S3, out IStorageProvider s3Provider))
            {
                Log.Error("S3 Provider not available");
            }
            configuredProvider = storageProvider == StorageProviders.File ? fileProvider : s3Provider;
            this.solrEngine = solrEngine;
        }

        /// <summary>
        /// Deletes all IIIF related files for a given manifest link from disk and solr
        /// </summary>
        /// <param name="manifestLink"></param>
        public async void DeleteIiifFiles(string manifestLink)
        {
            try
            {
                Log.Information("Delete Iiif files for manifestLink {manifestLink}", manifestLink);
                var key = manifestLink.Substring(0, manifestLink.LastIndexOf("/"));

                var relPath = key.Replace("/", "\\");

                var t1 = fileProvider.DeleteFolderAsync(Path.Combine(locationSettings.OcrOutputSaveDirectory, relPath));
                var t2 = fileProvider.DeleteFolderAsync(Path.Combine(locationSettings.ManifestOutputSaveDirectory, relPath));
                if (configuredProvider is S3Provider)
                {
                    var t3 = configuredProvider.DeleteFolderAsync("content\\" + relPath + "\\");
                    var t4 = configuredProvider.DeleteFolderAsync("images\\" + relPath + "\\");
                    Task.WaitAll(t1, t2, t3, t4);
                }
                else
                {
                    var t3 = fileProvider.DeleteFolderAsync(Path.Combine(locationSettings.ImageOutputSaveDirectory, relPath));
                    var t4 = fileProvider.DeleteFolderAsync(Path.Combine(locationSettings.ContentOutputSaveDirectory, relPath));
                    Task.WaitAll(t1, t2, t3, t4);
                }


                var solrResult = await solrEngine.DeleteDocumentsByQuery(key);

                if (!solrResult)
                {
                    Log.Warning("Solr delete not successful for ManifestLink: {manifestLink}", manifestLink);
                }
            }
            catch (Exception ex)
            {
                // Wenn das löschen scheitert wird nichts mehr unternommen
                Log.Warning(ex, "Error while deleting old IIIF files. Message is: {Message}", ex.Message);
            }
        }
    }
}
