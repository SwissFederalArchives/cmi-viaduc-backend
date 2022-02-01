using System.Linq;
using System.Net;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Management.api.Data;
using CMI.Web.Management.Auth;

namespace CMI.Web.Management.api.Controllers
{
    [Authorize]
    public class ConverterProgressController: ApiManagementControllerBase
    {
        private readonly AbbyyProgressInfo progressInfo;

        public ConverterProgressController(AbbyyProgressInfo progressInfo)
        {
            this.progressInfo = progressInfo;
        }

        [HttpGet]
        public IHttpActionResult GetCurrentConverterProgress()
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.ReportingStatisticsConverterProgressEinsehen);

            // To make sure we remove the completed items from the list here
            // Alternatively a timer should be used
            progressInfo.RemoveCompleted();

            // Calculate the state of the processes
            progressInfo.CalculateState();

            return Ok(progressInfo.ProgressDetails.Values.OrderBy(d => d.StartedOn));
        }

        [HttpPost]
        public IHttpActionResult ClearProgressInfo()
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.ReportingStatisticsConverterProgressEinsehen);

            progressInfo.RemoveAll();

            return StatusCode(HttpStatusCode.NoContent);
        }

        [HttpPost]
        public IHttpActionResult RemoveItemFromResults([FromBody] RemoveItemParameter p)
        {
            var access = this.GetManagementAccess();
            access.AssertFeatureOrThrow(ApplicationFeature.ReportingStatisticsConverterProgressEinsehen);

            progressInfo.RemoveItem(p.DetailId);

            return StatusCode(HttpStatusCode.NoContent);
        }
    }

    public class RemoveItemParameter
    {
        public string DetailId { get; set; }
    }
}