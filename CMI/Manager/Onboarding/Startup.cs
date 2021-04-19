using System.Web.Http;
using Ninject;
using Ninject.Web.Common.OwinHost;
using Ninject.Web.WebApi.OwinHost;
using Owin;
using Swashbuckle.Application;

namespace CMI.Manager.Onboarding
{
    public class Startup
    {
        public static StandardKernel Kernel { get; set; }

        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();

            config
                .EnableSwagger(c => c.SingleApiVersion("v1", "Swisscom Onboarding API"))
                .EnableSwaggerUi();

            config.MapHttpAttributeRoutes();

            app.UseWebApi(config);

            app
                .UseNinjectMiddleware(() => Kernel)
                .UseNinjectWebApi(config);
        }
    }
}