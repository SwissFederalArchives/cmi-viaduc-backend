using System.Web.Mvc;
using System.Web.Routing;
using Autofac;
using Autofac.Integration.Mvc;

namespace CMI.Web.Frontend
{
    public class WebConfig
    {
        public static void Configure()
        {
            RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            RegisterDependencyResolver();
        }

        private static void RegisterDependencyResolver()
        {
            var builder = new ContainerBuilder();

            DependencyResolver.SetResolver(new AutofacDependencyResolver(builder.Build()));
        }

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }
    }
}