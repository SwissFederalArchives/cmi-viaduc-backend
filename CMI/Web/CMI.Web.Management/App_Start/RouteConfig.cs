using System.Web.Mvc;
using System.Web.Routing;
using CMI.Web.Common.api;

namespace CMI.Web.Management
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");
            routes.MapRoute("PrivateAuthServices", "private/AuthServices/{action}", new {controller = "AuthServices"});
            routes.MapRoute("DefaultAuthServices", "AuthServices/{action}", new {controller = "AuthServices"});

            var excluded = string.Join("|", ApiHelper.WebApiSubRoot, "odata", "content", "token");

            routes.MapRoute(
                "Private",
                "private/{*url}",
                new {controller = "Home", action = "Index", url = string.Empty},
                new {url = $"^(?!({excluded})).*$"}
            );

            routes.MapRoute(
                "Default",
                "{*url}",
                new {controller = "Home", action = "Index", url = string.Empty},
                new {url = $"^(?!({excluded})).*$"}
            );
        }
    }
}