using System.Linq;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Contract.Common;
using CMI.Web.Common.api.Attributes;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;

namespace CMI.Web.Management.api.Controllers
{
    [NoCache]
    [Authorize]
    public class VManuelleKorrekturenController : ODataManagementControllerBase
    {
        private readonly IManuelleKorrekturAccess access;

        public VManuelleKorrekturenController(IManuelleKorrekturAccess access)
        {
            this.access = access;
        }

        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All, AllowedArithmeticOperators = AllowedArithmeticOperators.All,
            AllowedFunctions = AllowedFunctions.AllFunctions, AllowedLogicalOperators = AllowedLogicalOperators.All, MaxNodeCount = 500)]
        public IQueryable<VManuelleKorrektur> Get()
        {
            var userAccess = ManagementHelper.GetUserAccess();
            userAccess.AssertFeatureOrThrow(ApplicationFeature.AnonymisierungManuelleKorrekturenEinsehen);
            return access.Context.VManuelleKorrekturen;
        }
    }
}