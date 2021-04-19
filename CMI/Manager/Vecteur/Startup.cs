using System.Collections.Generic;
using System.Reflection;
using System.Web.Http;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Utilities.Template;
using Ninject;
using Ninject.Web.Common.OwinHost;
using Ninject.Web.WebApi.OwinHost;
using NSwag;
using NSwag.AspNet.Owin;
using NSwag.SwaggerGeneration.Processors.Security;
using Owin;
using Swashbuckle.Application;

namespace CMI.Manager.Vecteur
{
    public class Startup
    {
        public static IKernel Kernel { get; set; }

        public void Configuration(IAppBuilder app)
        {
            // siehe https://github.com/ninject/Ninject.Web.Common/wiki/Setting-up-a-OWIN-WebApi-application

            var config = new HttpConfiguration();
            config.Formatters.Insert(0, new DigitalisierungsAuftragFormatter());
            config
                .EnableSwagger(c => c.SingleApiVersion("v1", "Vecteur API"))
                .EnableSwaggerUi(c => { c.EnableApiKeySupport("X-ApiKey", "header"); });

            config.MapHttpAttributeRoutes();

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
                .UseNinjectMiddleware(CreateKernel)
                .UseNinjectWebApi(config);
        }

        private static StandardKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            kernel.Load(Assembly.GetExecutingAssembly());
            kernel.Bind<IMailHelper>().To<MailHelper>();
            kernel.Bind<IParameterHelper>().To<ParameterHelper>();
            kernel.Bind<IDataBuilder>().To<DataBuilder>();
            Kernel = kernel;
            return kernel;
        }
    }
}