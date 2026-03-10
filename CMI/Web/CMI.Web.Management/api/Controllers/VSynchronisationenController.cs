using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Contract.Common;
using CMI.Web.Common.api.Attributes;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;
using System.Linq;
using System.Web.Http;

namespace CMI.Web.Management.api.Controllers
{

    [NoCache]
    [Authorize]
    public class VSynchronisationenController : ODataManagementControllerBase
    {
        private readonly ISynchronisationAccess access;

        public VSynchronisationenController(ISynchronisationAccess access)
        {
            this.access = access;
        }

        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All, AllowedArithmeticOperators = AllowedArithmeticOperators.All,
            AllowedFunctions = AllowedFunctions.AllFunctions, AllowedLogicalOperators = AllowedLogicalOperators.All, MaxNodeCount = 500)]
        public IQueryable<VSyncAction> Get()
        {
            var userAccess = ManagementHelper.GetUserAccess();
            userAccess.AssertFeatureOrThrow(ApplicationFeature.SynchronizationUeberwachenEinsehen);
            return access.Context.VSyncActions;
        }
    }
}