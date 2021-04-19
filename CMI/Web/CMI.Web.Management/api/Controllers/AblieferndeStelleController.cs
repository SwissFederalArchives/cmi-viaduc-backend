using System.Net;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.AblieferndeStellen;
using CMI.Contract.Common;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Management.api.Data;
using CMI.Web.Management.Auth;

namespace CMI.Web.Management.api.Controllers
{
    /// <summary>
    ///     Im GUI wird die "Abliefernde Stelle" als "Zuständige Stelle" bezeichnet
    /// </summary>
    [Authorize]
    [NoCache]
    public class AblieferndeStelleController : ApiManagementControllerBase
    {
        private readonly IAblieferndeStelleDataAccess amtDataAccess;

        public AblieferndeStelleController(IAblieferndeStelleDataAccess amtDataAccess)
        {
            this.amtDataAccess = amtDataAccess;
        }

        [HttpGet]
        public IHttpActionResult GetAllAblieferndeStellen()
        {
            var data = amtDataAccess.GetAllAblieferndeStelle();
            return Ok(data);
        }

        [HttpGet]
        public IHttpActionResult GetAblieferndeStelle(int ablieferndeStelleId)
        {
            if (ablieferndeStelleId <= 0)
            {
                return BadRequest(nameof(ablieferndeStelleId));
            }

            var data = amtDataAccess.GetAblieferndeStelle(ablieferndeStelleId);
            return Ok(data);
        }

        [HttpPost]
        public IHttpActionResult CreateAblieferndeStelle([FromBody] AblieferndeStellePostData postData)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.BehoerdenzugriffZustaendigeStellenBearbeiten);

            if (string.IsNullOrEmpty(postData?.Bezeichnung))
            {
                return BadRequest(nameof(AblieferndeStellePostData.Bezeichnung));
            }

            if (string.IsNullOrEmpty(postData.Kuerzel))
            {
                return BadRequest(nameof(AblieferndeStellePostData.Kuerzel));
            }

            if (postData.TokenIdList == null || postData.TokenIdList.Count == 0)
            {
                return BadRequest(nameof(AblieferndeStellePostData.TokenIdList));
            }

            var result = amtDataAccess.CreateAblieferndeStelle(postData.Bezeichnung, postData.Kuerzel, postData.TokenIdList, postData.Kontrollstellen,
                access.UserId);
            return Ok(result);
        }

        [HttpPost]
        public IHttpActionResult DeleteAblieferndeStelle(int[] ablieferndeStelleIds)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.BehoerdenzugriffZustaendigeStellenBearbeiten);

            if (ablieferndeStelleIds == null || ablieferndeStelleIds.Length == 0)
            {
                return BadRequest(nameof(ablieferndeStelleIds));
            }

            var resultDelAmt = amtDataAccess.DeleteAblieferndeStelle(ablieferndeStelleIds);
            return Ok(resultDelAmt);
        }

        [HttpPost]
        public IHttpActionResult UpdateAblieferndeStelle([FromBody] AblieferndeStellePostData postData)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.BehoerdenzugriffZustaendigeStellenBearbeiten);

            if (string.IsNullOrEmpty(postData?.Bezeichnung))
            {
                return BadRequest(nameof(AblieferndeStellePostData.Bezeichnung));
            }

            if (string.IsNullOrEmpty(postData.Kuerzel))
            {
                return BadRequest(nameof(AblieferndeStellePostData.Kuerzel));
            }

            if (postData.TokenIdList == null || postData.TokenIdList.Count == 0)
            {
                return BadRequest(nameof(AblieferndeStellePostData.TokenIdList));
            }

            if (postData.AblieferndeStelleId <= 0)
            {
                return BadRequest(nameof(AblieferndeStellePostData.AblieferndeStelleId));
            }

            amtDataAccess.UpdateAblieferndeStelle(postData.AblieferndeStelleId, postData.Bezeichnung, postData.Kuerzel, postData.TokenIdList,
                postData.Kontrollstellen, access.UserId);
            return StatusCode(HttpStatusCode.NoContent);
        }
    }
}