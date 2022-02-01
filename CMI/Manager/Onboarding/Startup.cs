using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using CMI.Manager.Onboarding.Infrastructure;
using Owin;
using Swashbuckle.Application;

namespace CMI.Manager.Onboarding
{
    public class Startup
    {
        private static IContainer container;
        public static IContainer Container => container;

        public void Configuration(IAppBuilder app)
        {
            var builder = ContainerConfigurator.Configure();
            var config = new HttpConfiguration();

            config
                .EnableSwagger(c => c.SingleApiVersion("v1", "Swisscom Onboarding API"))
                .EnableSwaggerUi();

            config.MapHttpAttributeRoutes();

            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            
            container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            app.UseAutofacMiddleware(container);
            app.UseAutofacWebApi(config);
            app.UseWebApi(config);
        }
    }
}