using System;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Mvc;
using System.Web.Optimization;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common;
using CMI.Web.Common.api;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using CMI.Web.Management;
using CMI.Web.Management.api.Configuration;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSwag.AspNet.Owin;
using Owin;
using Serilog;
using Sustainsys.Saml2.Owin;
using SameSiteMode = Microsoft.Owin.SameSiteMode;
using Swashbuckle.Application;
using Microsoft.Owin.Host.SystemWeb;

[assembly: OwinStartup(typeof(Startup))]

namespace CMI.Web.Management
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            LogConfigurator.ConfigureForWeb(Assembly.GetExecutingAssembly().GetName().Name);

            Log.Information("Application_Start");
            ConfigureSecurity(app);
            UpgradeDb();
            CleanUpDb();

            AreaRegistration.RegisterAllAreas();

            GlobalConfiguration.Configure(cfg =>
            {
                cfg.Services.Add(typeof(IExceptionLogger), new CustomExceptionLogger());
                cfg.Services.Replace(typeof(IExceptionHandler), new CustomExceptionHandler());
                ODataConfig.Register(cfg);
                WebApiConfig.Register(cfg);
                EnableSwagger(app, cfg);
            });

            WebConfig.Configure();
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            RoutingHelper.Initialize();
        }

        private void EnableSwagger(IAppBuilder app, HttpConfiguration config)
        {
            config
                .EnableSwagger(c => c.SingleApiVersion("v1", "Schnittstelle Manager Client Viaduc"))
                .EnableSwaggerUi();
            app.UseSwaggerUi3(typeof(Startup).Assembly, settings =>
            {
                settings.GeneratorSettings.DefaultUrlTemplate = "api/{controller}/{action}/{id}";
                settings.GeneratorSettings.SerializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver()
                };
            });
        }

        private void CleanUpDb()
        {
            using (var cn = new SqlConnection(WebHelper.Settings["sqlConnectionString"]))
            {
                cn.Open();
                using (var cmd = cn.CreateCommand())
                {
                    var validnamesAsCommaSeparatedList = string.Join(",", Enum.GetNames(typeof(ApplicationFeature)).Select(name => "'" + name + "'"));
                    cmd.CommandText = $"DELETE FROM ApplicationRoleFeature WHERE FeatureId not IN ({validnamesAsCommaSeparatedList})";
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private void ConfigureSecurity(IAppBuilder app)
        {
            app.Use(async (context, next) => { await next.Invoke(); });

            var connectionString = ManagementSettingsViaduc.Instance.SqlConnectionString;
            var userDataAccess = new UserDataAccess(connectionString);

            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = WebHelper.CookieMcAppliationCookieKey,
                AuthenticationMode = AuthenticationMode.Active,
                CookieSameSite = SameSiteMode.Strict,
                CookieSecure = CookieSecureOption.Always,
                CookieHttpOnly = true,
                ExpireTimeSpan = TimeSpan.FromMinutes(ManagementSettingsViaduc.Instance.CookieExpireTimeInMinutes),
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
                SPOptions = {Logger = new SeriLogAdapter(Log.Logger)}
            };

            var authServiceNotifications = new AuthServiceNotifications(authOptions.SPOptions, false);
            authOptions.Notifications.AcsCommandResultCreated += authServiceNotifications.AcsCommandResultCreated;
            authOptions.Notifications.SelectIdentityProvider += authServiceNotifications.SelectIdentityProvider;
            authOptions.Notifications.AuthenticationRequestCreated += authServiceNotifications.AuthenticationRequestCreated;

            app.UseSaml2Authentication(authOptions);

            app.Use(async (context, next) => { await next.Invoke(); });

            Log.Information("ConfigureSecurity: tokenExpiry={cookieExpireTimeInMinutes}", ManagementSettingsViaduc.Instance.CookieExpireTimeInMinutes);
        }

        private void UpgradeDb()
        {
            var dbUpgrader = new DbUpgrader(WebHelper.Settings["sqlConnectionString"]);
            try
            {
                var info = dbUpgrader.Upgrade();
                Log.Information($"Startup.UpgradeDb: version={info}", info);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Startup.UpgradeDb failed");
                throw;
            }
        }

        private static Task ValidateSessionIdIsActive(CookieValidateIdentityContext context, UserDataAccess userDataAccess)
        {
            var userId = context.Identity.Claims.FirstOrDefault(c => c.Type.Contains(ClaimValueNames.UserExtId))?.Value;
            var user = userDataAccess.GetUser(userId);

            var activeAspNetSessionId = user?.ActiveAspNetSessionId;
            var currentAspNetSessionId = context.Request.Cookies[WebHelper.CookieMcAspNetSessionIdKey];

            if (currentAspNetSessionId != activeAspNetSessionId)
            {
                context.RejectIdentity();
                context.OwinContext.Authentication.SignOut(context.Options.AuthenticationType);
            }

            return Task.CompletedTask;
        }
    }
}