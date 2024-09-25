using System;
using System.Web.Http;
using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Web.Management.api.Controllers;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;

namespace CMI.Web.Management
{
    public class ODataConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.MapHttpAttributeRoutes();

            var modelBuilder = new ODataConventionModelBuilder();
            modelBuilder.EnableLowerCamelCase();

            var orderingName = nameof(OrderingFlatItemsController).Replace("Controller", "");
            var userOverviewName = nameof(UserOverviewController).Replace("Controller", "");
            var manuelleKorrekturName = nameof(VManuelleKorrekturenController).Replace("Controller", "");

            modelBuilder.EntitySet<OrderingFlatItem>(orderingName).EntityType.Count().Select().Filter().Expand().Page().OrderBy();
            modelBuilder.EntitySet<UserOverview>(userOverviewName).EntityType.Count().Select().Filter().Expand().Page().OrderBy();
            modelBuilder.EntitySet<VManuelleKorrektur>(manuelleKorrekturName).EntityType.Count().Select().Filter().Expand().Page().OrderBy();

            config.MapODataServiceRoute(
                "ODataRoute",
                "odata",
                modelBuilder.GetEdmModel(),
                new DefaultODataBatchHandler(GlobalConfiguration.DefaultServer));

            config.SetTimeZoneInfo(TimeZoneInfo.Local);
            config.EnsureInitialized();
        }
    }
}