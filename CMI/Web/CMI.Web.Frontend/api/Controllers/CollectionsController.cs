using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using CMI.Web.Common.Helpers;

namespace CMI.Web.Frontend.api.Controllers
{
    public class CollectionsController : ApiFrontendControllerBase
    {
        private readonly ICollectionManager collectionManager;

        public CollectionsController(ICollectionManager collectionManager)
        {
            this.collectionManager = collectionManager;
        }

        [HttpGet]
        public async Task<List<CollectionListItemDto>> GetActiveCollections(int? parentId, string language)
        {
            return await collectionManager.GetActiveCollections(parentId, language);
        }

        [HttpGet]
        public async Task<CollectionItemResult> Get(int id)
        {
            var language = WebHelper.GetClientLanguage(Request);
            var result = await collectionManager.GetCollectionItemResult(id);
            if (result != null && result.Item?.Language == language)
            { 
                return result; 
            }

            return new CollectionItemResult();
        }

        [HttpGet]
        public async Task<string> GetCollectionsHeader(string language)
        {
           return await collectionManager.GetCollectionsHeader(language);
        }

       [HttpGet]
        public async Task<IHttpActionResult> GetSizedImage(int id, string mimeType, int width, int height)
        {
            var imageInfo = await collectionManager.GetImage(id, false, mimeType, new Size(width, height));
            var ms = new MemoryStream(imageInfo.Image);
            ms.Position = 0;

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(ms)
            };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(imageInfo.MimeType);
            return ResponseMessage(result);
        }

        [HttpGet]
        public async Task<IHttpActionResult> GetImage(int id, bool usePrecalculatedThumbnail)
        {
            var imageInfo = await collectionManager.GetImage(id, usePrecalculatedThumbnail);
            var ms = new MemoryStream(imageInfo.Image);
            ms.Position = 0;

            var result = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(ms)
            };
            result.Content.Headers.ContentType = new MediaTypeHeaderValue(imageInfo.MimeType);
            return ResponseMessage(result);
        }
    }
}