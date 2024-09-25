using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Mvc;
using System.Web.Optimization;
using CMI.Access.Sql.Viaduc;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common;
using CMI.Web.Common.api;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Controllers;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Host.SystemWeb;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSwag;
using NSwag.AspNet.Owin;
using Owin;
using Serilog;
using Sustainsys.Saml2.Owin;
using SameSiteMode = Microsoft.Owin.SameSiteMode;

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

            app.UseSwaggerUi3(new[] { typeof(ExternalController) }, settings =>
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
            });


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

            var connectionString = FrontendSettingsViaduc.Instance.SqlConnectionString;
            var userDataAccess = new UserDataAccess(connectionString);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = WebHelper.CookiePcAppliationCookieKey,
                AuthenticationMode = AuthenticationMode.Active,
                CookieSameSite = SameSiteMode.Strict,
                CookieSecure = CookieSecureOption.Always,
                CookieHttpOnly = true,
                ExpireTimeSpan = TimeSpan.FromMinutes(FrontendSettingsViaduc.Instance.CookieExpireTimeInMinutes),
                SlidingExpiration = true,
                Provider = new CookieAuthenticationProvider
                {
                    OnValidateIdentity = context => ValidateSessionIdIsActive(context, userDataAccess)
                },
                CookieManager = new SameSiteCookieManager(new SystemWebCookieManager())
            });

            app.Use(async (context, next) => { await next.Invoke(); });

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            app.Use(async (context, next) => { await next.Invoke(); });

            var authOptions = new Saml2AuthenticationOptions(true)
            {
                SPOptions = { Logger = new SeriLogAdapter(Log.Logger)}
            };
            
            var authServiceNotifications = new AuthServiceNotifications(authOptions.SPOptions,  true);
            authOptions.Notifications.AcsCommandResultCreated += authServiceNotifications.AcsCommandResultCreated;
            authOptions.Notifications.SelectIdentityProvider += authServiceNotifications.SelectIdentityProvider;
            authOptions.Notifications.AuthenticationRequestCreated += authServiceNotifications.AuthenticationRequestCreated;

            app.UseSaml2Authentication(authOptions);

            Log.Information("ConfigureSecurity: tokenExpiry={cookieExpireTimeInMinutes}",
                FrontendSettingsViaduc.Instance.CookieExpireTimeInMinutes);

            app.Use(async (context, next) => { await next.Invoke(); });
        }

        private static Task ValidateSessionIdIsActive(CookieValidateIdentityContext context, UserDataAccess userDataAccess)
        {
            var userId = context.Identity.Claims.FirstOrDefault(c => c.Type.Contains(ClaimValueNames.UserExtId))?.Value;
            var user = userDataAccess.GetUser(userId);

            var activeAspNetSessionId = user?.ActiveAspNetSessionId;
            var currentAspNetSessionId = context.Request.Cookies[WebHelper.CookiePcAspNetSessionIdKey];

            if (currentAspNetSessionId != activeAspNetSessionId)
            {
                context.RejectIdentity();
                context.OwinContext.Authentication.SignOut(context.Options.AuthenticationType);
            }

            return Task.CompletedTask;
        }
    }
}

