using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    public class ManuelleKorrekturenController : ApiManagementControllerBase
    {
        
        private readonly IManuelleKorrekturManager manuelleKorrekturManagerClient;

        public ManuelleKorrekturenController(IManuelleKorrekturManager manuelleKorrekturManagerClient)
        {
            this.manuelleKorrekturManagerClient = manuelleKorrekturManagerClient;
        }

        [HttpGet]
        public async Task<ManuelleKorrekturDetailItem> GetManuelleKorrektur(int id)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AnonymisierungManuelleKorrekturenBearbeiten);
            return await manuelleKorrekturManagerClient.GetManuelleKorrektur(id);
        }

        [HttpPost]
        public async Task<ManuelleKorrekturDto> InsertOrUpdateManuelleKorrektur(ManuelleKorrekturDto manuelleKorrektur)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AnonymisierungManuelleKorrekturenBearbeiten);

            return await manuelleKorrekturManagerClient.InsertOrUpdateManuelleKorrektur(manuelleKorrektur, access.UserId);
        }

        [HttpDelete]
        public async Task<IHttpActionResult> Delete(int id)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AnonymisierungManuelleKorrekturenBearbeiten);
            await manuelleKorrekturManagerClient.DeleteManuelleKorrektur(id);

            return Ok();
        }

        [HttpPost]
        public async Task<Dictionary<string, string>> BatchAddManuelleKorrektur(string[] veIds)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AnonymisierungManuelleKorrekturenBearbeiten);
            return await manuelleKorrekturManagerClient.BatchAddManuelleKorrektur(veIds.Select(WebUtility.UrlDecode).ToArray(), access.UserId);
        }

        [HttpPost]
        public async Task<IHttpActionResult> BatchDeleteManuelleKorrektur([FromBody] int[] manuelleKorrekturIds)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AnonymisierungManuelleKorrekturenBearbeiten);
            await manuelleKorrekturManagerClient.BatchDeleteManuelleKorrektur(manuelleKorrekturIds);
            return Ok();
        }

        [HttpGet]
        public async Task<ManuelleKorrekturDto> Publizieren(int id)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AnonymisierungManuelleKorrekturenBearbeiten);
            return await manuelleKorrekturManagerClient.PublizierenManuelleKorrektur(id, access.UserId);
        }

    }
}
