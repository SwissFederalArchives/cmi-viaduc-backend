using System.Web.Http;
using CMI.Web.Common.api;

namespace CMI.Web.Frontend
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.EnableCors();

            // Attribute based routing
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                "DefaultApi",
                ApiHelper.WebApiSubRoot + "/{controller}/{action}/{id}",
                new {id = RouteParameter.Optional}
            );

            config.Routes.MapHttpRoute(
                "ErrorApi404",
                ApiHelper.WebApiSubRoot + "/{*url}",
                new {lang = "de", controller = "Error", action = "NotFound"},
                new {lang = @"(de|fr|it|en)"}
            );
        }
    }
}