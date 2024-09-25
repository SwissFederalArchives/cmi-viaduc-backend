using System.Web.Mvc;
using System.Web.Routing;

namespace CMI.Web.Frontend
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.RouteExistingFiles = true;
            
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "AuthServices",
                "AuthServices/{action}",
                new {controller = "AuthServices"}
            );

            routes.MapRoute(
                "Private",
                "private",
                new {controller = "Private", action = "RedirectToSignIn"}
            );

            routes.MapRoute(
                "Error",
                "{lang}/error",
                new {lang = "de", controller = "Error", action = "Index"},
                new {lang = @"(de|fr|it|en)"}
            );

            routes.MapRoute(
                "Content",
                "{prefix}/{*url}",
                new {controller = "Content", action = "Index"},
                new
                {
                    prefix = "(de|fr|it|en|content)",
                    url = "^([^.]+)(\\.html)?$"
                }
            );

            routes.MapRoute(
                "IIIFContent",
                "files/{subdirectory}/{*relativeFile}",
                new { controller = "Files", action = "Index" },
                new
                {
                    subdirectory = "(content|manifests|ocr)",
                    relativeFile = "^(\\S)+?\\.(txt|pdf|svg|json|zip)$"
                }
            );

            routes.MapRoute(
                "Default",
                "{lang}",
                new {lang = "de", controller = "Home", action = "Index"},
                new {lang = @"(de|fr|it|en)"}
            );

           
        }
    }
}