using System;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Http;
using Autofac;
using Autofac.Integration.WebApi;
using CMI.Tools.AnonymizeServiceMock.Infrastructure;
using NSwag;
using NSwag.AspNet.Owin;
using NSwag.Generation.Processors.Security;
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

            var config = new HttpConfiguration();
            config
                .EnableSwagger(c => c.SingleApiVersion("v1", "Schnittstelle Viaduc Anonymisierung"))
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
                    document.Info.Description = "Ein Dienst um Text schwärzen zu lassen. Der Dienst anonymisiert Namen von natürlichen und juristischen Personen. Des wWeiteren werden weitere persönliche Identifikationsmerkmale wie Geburtstdatum oder AHV-Nummern erkannt.";
                    document.SecurityDefinitions.Add("ApiKey", new OpenApiSecurityScheme
                    {
                        Type = OpenApiSecuritySchemeType.ApiKey,
                        Scheme = "X-ApiKey",
                        In = OpenApiSecurityApiKeyLocation.Header,
                        Name = "X-ApiKey",
                        Description = "API key for request authorization."
                    });

                    document.Info.Contact = new OpenApiContact
                    {
                        Email = "jlang@evelix.ch",
                        Name = "Jörg Lang"
                    };

                    document.Tags.Add(new OpenApiTag
                    {
                        Name = "Anonymisierung",
                        Description = "Gesicherte Operationen für die Anonymisierung eines Textes"
                    });

                    document.Tags.Add(new OpenApiTag
                    {
                        Name = "anonymizeText",
                        Description = "Erweitert einen Text mit Anonymisierungs" + Environment.NewLine +
                                      "Es kann ein Textstring mit maximal 200'000 Zeichen übergeben werden. Der Text wird anonymisiert und die kritischen Stellen ausgezeichnet."
                    });

                    document.Produces = new List<string> { "application/json" };
                    document.Consumes = new List<string> { "application/json" };
                };
            })
                .UseSwaggerUi3(typeof(Startup).Assembly, c =>
                {
                    c.GeneratorSettings.DocumentProcessors.Add(
                        new SecurityDefinitionAppender("ApiKey", new OpenApiSecurityScheme
                        {
                            Type = OpenApiSecuritySchemeType.ApiKey,
                            Scheme = "X-ApiKey",
                            In = OpenApiSecurityApiKeyLocation.Header,
                            Name = "X-ApiKey",
                            Description = "API key for request authorization."
                        }));
                    c.GeneratorSettings.OperationProcessors.Add(new OperationSecurityScopeProcessor("ApiKey"));
                })
                .UseAutofacMiddleware(Container)
                .UseAutofacWebApi(config)
                .UseWebApi(config);
        }
    }
}