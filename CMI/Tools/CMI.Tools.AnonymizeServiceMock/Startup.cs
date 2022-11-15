using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using CMI.Tools.AnonymizeServiceMock.Infrastructure;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSwag;
using NSwag.AspNet.Owin;
using NSwag.SwaggerGeneration.Processors.Security;
using Owin;
using Swashbuckle.Application;

namespace CMI.Tools.AnonymizeServiceMock
{
    public class Startup
    {
        public static IContainer Container { get; private set; }

        public void Configuration(IAppBuilder app)
        {
            var builder = ContainerConfigurator.Configure();

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            var config = new HttpConfiguration();
            config
                .EnableSwagger(c => {
                    c.SingleApiVersion("v1", "Schnittstelle Viaduc Anonymisierung");
                    c.DescribeAllEnumsAsStrings();
                    })
                .EnableSwaggerUi(c => { c.EnableApiKeySupport("X-ApiKey", "header"); });

            config.MapHttpAttributeRoutes();
            builder.RegisterApiControllers(Assembly.GetExecutingAssembly());

            Container = builder.Build();
            config.DependencyResolver = new AutofacWebApiDependencyResolver(Container);

            app.UseSwagger(typeof(Startup).Assembly, c =>
                {
                    c.PostProcess = document =>
                    {
                        document.Info.Title = "Schnittstelle Viaduc Anonymisierung";
                        document.Info.Version = "1.0.0";
                        document.Info.Description = "Ein Dienst um Text schwärzen zu lassen. Der Dienst anonymisiert Namen von natürlichen und juristischen Personen. Des Weiteren werden weitere persönliche Identifikationsmerkmale wie Geburtstdatum oder AHV-Nummern erkannt.";
                        document.SecurityDefinitions.Add("ApiKey", new SwaggerSecurityScheme
                        {
                            Type = SwaggerSecuritySchemeType.ApiKey,
                            Scheme = "X-ApiKey",
                            In = SwaggerSecurityApiKeyLocation.Header,
                            Name = "X-ApiKey",
                            Description = "API key for request authorization."
                        });

                        document.Info.Contact = new SwaggerContact
                        {
                            Email = "jlang@evelix.ch",
                            Name = "Jörg Lang"
                        };

                        document.Tags.Add(new SwaggerTag
                        {
                            Name = "Anonymisierung",
                            Description = "Gesicherte Operationen für die Anonymisierung eines Textes"
                        });


                        document.Produces = new List<string> { "application/json" };
                        document.Consumes = new List<string> { "application/json" };
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
                .UseAutofacMiddleware(Container)
                .UseAutofacWebApi(config)
                .UseWebApi(config);
        }
    }
}