using System;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Mvc;
using System.Web.Optimization;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend;
using CMI.Web.Frontend.api.Controllers;
using Kentor.AuthServices.Owin;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NJsonSchema.Generation;
using NSwag;
using NSwag.AspNet.Owin;
using NSwag.SwaggerGeneration;
using Owin;
using Serilog;

[assembly: OwinStartup(typeof(Startup))]

namespace CMI.Web.Frontend
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            LogConfigurator.ConfigureForWeb(Assembly.GetExecutingAssembly().GetName().Name);

            Log.Information("Application_Start");

            ConfigureSecurity(app);

            app.UseSwaggerUi3(new[] {typeof(ExternalController)}, settings =>
            {
                settings.GeneratorSettings.DefaultUrlTemplate = "api/{controller}/{action}/{id?}";
                settings.GeneratorSettings.Title = "Viaduc REST API";
                settings.GeneratorSettings.Description = @"The API lets you search the Swiss Federal Archives.";
                settings.GeneratorSettings.Version = "1.0";
                settings.PostProcess = doc =>
                {
                    doc.Servers.Clear();
                    doc.Servers.Add(new OpenApiServer
                    {
                        // Basis-URL der Doku auf BAR-Umgebungen stimmen sonst nicht
                        Url = WebHelper.SwaggerBaseUrl
                    });
                };
                settings.GeneratorSettings.SerializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
            }, new SwaggerJsonSchemaGenerator(new JsonSchemaGeneratorSettings()));


            WebConfig.Configure();
            AreaRegistration.RegisterAllAreas();

            GlobalConfiguration.Configure(cfg =>
            {
                cfg.Services.Add(typeof(IExceptionLogger), new CustomExceptionLogger());
                cfg.Services.Replace(typeof(IExceptionHandler), new CustomExceptionHandler());
                WebApiConfig.Register(cfg);
            });

            BundleConfig.RegisterBundles(BundleTable.Bundles);

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            RoutingHelper.Initialize();
        }

        private void ConfigureSecurity(IAppBuilder app)
        {
            app.Use(async (context, next) => { await next.Invoke(); });

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            app.Use(async (context, next) => { await next.Invoke(); });

            var authOptions = new KentorAuthServicesAuthenticationOptions(true)
            {
                SPOptions = {Logger = new SeriLogAdapter(Log.Logger)}
            };

            var authServiceNotifications = new AuthServiceNotifications(authOptions.SPOptions, true);
            authOptions.Notifications.AcsCommandResultCreated += authServiceNotifications.AcsCommandResultCreated;

            app.UseKentorAuthServicesAuthentication(authOptions);

            app.Use(async (context, next) => { await next.Invoke(); });

            Log.Information("ConfigureSecurity: tokenExpiry={TokenExpiryInMinutes}",
                AuthenticationHelper.TokenExpiryInMinutes);

            var oAuthAuthorizationServerOptions = new OAuthAuthorizationServerOptions
            {
                AllowInsecureHttp = true,
                TokenEndpointPath = new PathString("/token"),
                AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(AuthenticationHelper.TokenExpiryInMinutes),
                Provider = new AuthorizationServerProvider()
            };

            app.UseOAuthAuthorizationServer(oAuthAuthorizationServerOptions);

            app.Use(async (context, next) => { await next.Invoke(); });

            var authNOptions = AuthenticationHelper.WebApiBearerAuthenticationOptions =
                new OAuthBearerAuthenticationOptions();
            app.UseOAuthBearerAuthentication(authNOptions);

            app.Use(async (context, next) => { await next.Invoke(); });
        }
    }
}