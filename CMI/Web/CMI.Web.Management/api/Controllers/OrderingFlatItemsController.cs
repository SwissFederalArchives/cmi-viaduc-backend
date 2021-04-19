using System.Linq;
using System.Net;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Contract.Order;
using CMI.Web.Common.api.Attributes;
using CMI.Web.Common.Helpers;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Query;

namespace CMI.Web.Management.api.Controllers
{
    [NoCache]
    [Authorize]
    public class OrderingFlatItemsController : ODataManagementControllerBase
    {
        [EnableQuery(AllowedQueryOptions = AllowedQueryOptions.All, AllowedArithmeticOperators = AllowedArithmeticOperators.All,
            AllowedFunctions = AllowedFunctions.AllFunctions, AllowedLogicalOperators = AllowedLogicalOperators.All, MaxNodeCount = 500)]
        public IHttpActionResult Get()
        {
            var ctx = new ViaducContext(WebHelper.Settings["sqlConnectionString"]);
            var access = ManagementHelper.GetUserAccess();

            var items = ctx.OrderingFlatItem.AsQueryable()
                .Where(o => o.Status != (int) OrderStatesInternal.ImBestellkorb);

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtEinsichtsgesucheView))
            {
                items = items.Where(i => i.OrderingType != (int) OrderType.Einsichtsgesuch);
            }

            if (!access.HasFeature(ApplicationFeature.AuftragsuebersichtAuftraegeView))
            {
                items = items.Where(i => i.OrderingType != (int) OrderType.Digitalisierungsauftrag
                                         && i.OrderingType != (int) OrderType.Lesesaalausleihen
                                         && i.OrderingType != (int) OrderType.Verwaltungsausleihe);
            }

            return items.Any()
                ? Ok(items)
                : (IHttpActionResult) StatusCode(HttpStatusCode.NoContent);
        }
    }
}