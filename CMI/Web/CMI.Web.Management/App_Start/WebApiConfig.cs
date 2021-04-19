using System.Web.Http;
using CMI.Web.Common.api;

namespace CMI.Web.Management
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            config.EnableCors();

            config.Routes.MapHttpRoute(
                "DefaultApi",
                ApiHelper.WebApiSubRoot + "/{controller}/{action}/{id}",
                new {id = RouteParameter.Optional}
            );
        }
    }
}