using System;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.ExceptionHandling;
using System.Web.Mvc;
using System.Web.Optimization;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.Logging.Configurator;
using CMI.Web.Common;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using CMI.Web.Management;
using CMI.Web.Management.App_Start;
using Kentor.AuthServices.Owin;
using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.OAuth;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Owin;
using Serilog;

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

            BusConfig.Configure(NinjectWebCommon.Kernel);
            AreaRegistration.RegisterAllAreas();

            GlobalConfiguration.Configure(cfg =>
            {
                cfg.Services.Add(typeof(IExceptionLogger), new CustomExceptionLogger());
                cfg.Services.Replace(typeof(IExceptionHandler), new CustomExceptionHandler());
                ODataConfig.Register(cfg);
                WebApiConfig.Register(cfg);
            });

            WebConfig.Configure();

            BundleConfig.RegisterBundles(BundleTable.Bundles);

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            RoutingHelper.Initialize();
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

            app.UseExternalSignInCookie(DefaultAuthenticationTypes.ExternalCookie);

            app.Use(async (context, next) => { await next.Invoke(); });

            var authOptions = new KentorAuthServicesAuthenticationOptions(true)
            {
                SPOptions = {Logger = new SeriLogAdapter(Log.Logger)}
            };

            var authServiceNotifications = new AuthServiceNotifications(authOptions.SPOptions, false);
            authOptions.Notifications.AcsCommandResultCreated += authServiceNotifications.AcsCommandResultCreated;

            app.UseKentorAuthServicesAuthentication(authOptions);

            app.Use(async (context, next) => { await next.Invoke(); });

            Log.Information("ConfigureSecurity: tokenExpiry={TokenExpiryInMinutes}", AuthenticationHelper.TokenExpiryInMinutes);

            var oAuthAuthorizationServerOptions = new OAuthAuthorizationServerOptions
            {
                AllowInsecureHttp = true,
                TokenEndpointPath = new PathString("/token"),
                AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(AuthenticationHelper.TokenExpiryInMinutes),
                Provider = new AuthorizationServerProvider()
            };

            app.UseOAuthAuthorizationServer(oAuthAuthorizationServerOptions);

            app.Use(async (context, next) => { await next.Invoke(); });

            var authNOptions = AuthenticationHelper.WebApiBearerAuthenticationOptions = new OAuthBearerAuthenticationOptions();
            app.UseOAuthBearerAuthentication(authNOptions);

            app.Use(async (context, next) => { await next.Invoke(); });
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
    }
}