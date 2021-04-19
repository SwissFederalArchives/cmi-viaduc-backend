using System.Linq;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;

namespace CMI.Web.Management.api.Controllers
{
    [NoCache]
    [Authorize]
    public class UserOverviewController : ODataManagementControllerBase
    {
        [EnableQuery(EnsureStableOrdering = false, AllowedQueryOptions = AllowedQueryOptions.All,
            AllowedArithmeticOperators = AllowedArithmeticOperators.All, AllowedFunctions = AllowedFunctions.AllFunctions,
            AllowedLogicalOperators = AllowedLogicalOperators.All, MaxNodeCount = 500)]
        public IHttpActionResult Get()
        {
            var ctx = new ViaducContext(WebHelper.Settings["sqlConnectionString"]);
            return Ok(ctx.UserOverview
                .OrderBy(u => u.FamilyName)
                .ThenBy(u => u.FirstName)
                .AsQueryable());
        }
    }
}