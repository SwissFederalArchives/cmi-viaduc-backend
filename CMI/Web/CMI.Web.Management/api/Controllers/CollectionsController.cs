using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    public class CollectionsController : ApiManagementControllerBase
    {
        private readonly ICollectionManager collectionManager;

        public CollectionsController(ICollectionManager collectionManager)
        {
            this.collectionManager = collectionManager;
        }

        [HttpGet]
        public async Task<IEnumerable<CollectionListItemDto>> GetAll()
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationSammlungenEinsehen);
            return await collectionManager.GetAllCollections();
        }

        [HttpGet]
        public async Task<List<DropDownListItem>> GetPossibleParents(int id)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationSammlungenEinsehen);
            return await collectionManager.GetPossibleParents(id);
        }

        [HttpGet]
        public async Task<CollectionDto> Get(int id)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationSammlungenEinsehen);
            return await collectionManager.GetCollection(id);
        }

        [HttpGet]
        [AllowAnonymous]
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
        [AllowAnonymous]
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


        [HttpPost]
        public async Task<IHttpActionResult> Create([FromBody] CollectionDto value)
        {
            if (value == null)
            {
                return BadRequest();
            }

            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationSammlungenBearbeiten);

            var item = await collectionManager.InsertOrUpdateCollection(value, access.UserId);
            return Ok(item);
        }

        [HttpPut]
        public async Task<IHttpActionResult> Update(int id, [FromBody] CollectionDto value)
        {
            if (value == null || id != value.CollectionId)
            {
                return BadRequest();
            }

            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationSammlungenBearbeiten);

            var item = await collectionManager.InsertOrUpdateCollection(value, access.UserId);
            return Ok(item);
        }

        [HttpDelete]
        public async Task<IHttpActionResult> Delete(int id)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationSammlungenBearbeiten);

            await collectionManager.DeleteCollection(id);

            return Ok();
        }

        [HttpPost]
        public async Task<IHttpActionResult> BatchDelete([FromBody] int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return BadRequest();
            }

            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AdministrationSammlungenBearbeiten);

            await collectionManager.BatchDeleteCollection(ids);
            return Ok();
        }
    }
}