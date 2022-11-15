using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Common.Entities;

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
        public Task<ManuelleKorrekturDetailItem> GetManuelleKorrektur(int id)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AnonymisierungManuelleKorrekturenBearbeiten);
            return manuelleKorrekturManagerClient.GetManuelleKorrektur(id);
        }

        [HttpPost]
        public Task<ManuelleKorrekturDto> InsertOrUpdateManuelleKorrektur(ManuelleKorrekturDto manuelleKorrektur)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AnonymisierungManuelleKorrekturenBearbeiten);

            return manuelleKorrekturManagerClient.InsertOrUpdateManuelleKorrektur(manuelleKorrektur, access.UserId);
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
            return await manuelleKorrekturManagerClient.BatchAddManuelleKorrektur(veIds, access.UserId);
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
        public Task<ManuelleKorrekturDto> Publizieren(int id)
        {
            var access = ManagementControllerHelper.GetUserAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.AnonymisierungManuelleKorrekturenBearbeiten);
            return manuelleKorrekturManagerClient.PublizierenManuelleKorrektur(id, access.UserId);
        }

    }
}
