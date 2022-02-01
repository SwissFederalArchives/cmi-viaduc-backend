using System.Collections.Generic;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using Autofac;
using Autofac.Integration.WebApi;
using CMI.Manager.Vecteur.Infrastructure;
using NSwag;
using NSwag.AspNet.Owin;
using NSwag.SwaggerGeneration.Processors.Security;
using Owin;
using Swashbuckle.Application;

namespace CMI.Manager.Vecteur
{
    public class Startup
    {
        private static IContainer container;

        public static IContainer Container => container;

        public void Configuration(IAppBuilder app)
        {
            var builder = ContainerConfigurator.Configure();

            var config = new HttpConfiguration();
            config.Formatters.Insert(0, new DigitalisierungsAuftragFormatter());
            config
                .EnableSwagger(c => c.SingleApiVersion("v1", "Vecteur API"))
                .EnableSwaggerUi(c => { c.EnableApiKeySupport("X-ApiKey", "header"); });

            config.MapHttpAttributeRoutes();
            config.Services.Add(typeof(IExceptionLogger), new VecteurExceptionLogger());
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());
            
            container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(container);

            app
                .UseSwagger(typeof(Startup).Assembly, c =>
                {
                    c.PostProcess = document =>
                    {
                        document.Info.Title = "Vecteur API";
                        document.Info.Version = "v1";
                        document.Info.Description = "API um Aufträge aus Viaduc zu verarbeiten.";
                        document.SecurityDefinitions.Add("ApiKey", new SwaggerSecurityScheme
                        {
                            Type = SwaggerSecuritySchemeType.ApiKey,
                            Scheme = "X-ApiKey",
                            In = SwaggerSecurityApiKeyLocation.Header,
                            Name = "X-ApiKey",
                            Description = "API key for request authorization."
                        });
                        document.Produces = new List<string> {"application/xml", "application/json"};
                        document.Consumes = new List<string> {"application/xml", "application/json"};
                    };
                })
                .UseSwaggerUi3(typeof(Startup).Assembly, c =>
                {
                    c.GeneratorSettings.DocumentProcessors.Add(
                        new SecurityDefinitionAppender("ApiKey", new SwaggerSecurityScheme
                        {
                            Type = SwaggerSecuritySchemeType.ApiKey,
                            Name = "X-ApiKey",
                            In = SwaggerSecurityApiKeyLocation.Header
                        }));
                    c.GeneratorSettings.OperationProcessors.Add(new OperationSecurityScopeProcessor("ApiKey"));
                })
                .UseAutofacMiddleware(container)
                .UseAutofacWebApi(config)
                .UseWebApi(config);
        }

    }
}