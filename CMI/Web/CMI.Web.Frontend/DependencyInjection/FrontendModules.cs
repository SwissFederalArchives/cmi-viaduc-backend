using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.File;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Utilities.Cache.Access;
using CMI.Utilities.ProxyClients.Order;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using CMI.Web.Frontend.api;
using CMI.Web.Frontend.api.Configuration;
using CMI.Web.Frontend.api.Controllers;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Entities;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Providers;
using CMI.Web.Frontend.Helpers;
using CMI.Web.Frontend.ParameterSettings;
using MassTransit;
using Newtonsoft.Json.Linq;
using Ninject;
using Ninject.Modules;
using Ninject.Web.Common;

namespace CMI.Web.Frontend.DependencyInjection
{
    public class FrontendModules : NinjectModule
    {
        /// <summary>Loads the module into the kernel.</summary>
        /// <devdoc>
        ///     Der Scope wird auf .InRequestScope() festgelegt, damit pro Request pro Klasse exakt eine neue Instanz pro Klasse
        ///     erstellt wird.
        /// </devdoc>
        public override void Load()
        {
            var connectionString = FrontendSettingsViaduc.Instance.SqlConnectionString;
            var sftpLicensingKey = WebHelper.Settings["sftpLicenseKey"];

            Bind<IParameterHelper>().To<ParameterHelper>();
            Bind<DigitalisierungsbeschraenkungSettings>()
                .ToMethod(ctx => ctx.Kernel.Get<IParameterHelper>().GetSetting<DigitalisierungsbeschraenkungSettings>())
                .InTransientScope();

            Bind<ManagementClientSettings>()
                .ToMethod(ctx => ctx.Kernel.Get<IParameterHelper>().GetSetting<ManagementClientSettings>())
                .InTransientScope();

            Bind<VerwaltungsausleiheSettings>()
                .ToMethod(ctx => ctx.Kernel.Get<IParameterHelper>().GetSetting<VerwaltungsausleiheSettings>())
                .InTransientScope();

            Bind<AutomatischeBenachrichtigungAnKontrollstelle>()
                .ToMethod(ctx => ctx.Kernel.Get<IParameterHelper>().GetSetting<AutomatischeBenachrichtigungAnKontrollstelle>())
                .InTransientScope();

            Bind<IElasticSettings>().ToMethod(ctx => ServerSettings.GetServerSettings<ElasticSettings>());
            Bind<IElasticClientProvider>().To<ElasticClientProvider>().InRequestScope();
            Bind<IElasticService>().To<ElasticService>().InSingletonScope();
            Bind<IFileSystem>().To<PhysicalFileSystem>().InSingletonScope();

            Bind<IEntityProvider>().To<EntityProvider>().InRequestScope();
            Bind<IModelData>().To<ModelData>();
            Bind<ICmiSettings>().To<CmiSettings>();
            Bind<IWebCmiConfigProvider>().To<WebCmiConfigProvider>();
            Bind<IKontrollstellenInformer>().To<KontrollstellenInformer>();

            Bind<IPublicOrder>().To<OrderManagerClient>();
            Bind<IFileDownloadHelper>().To<FileDownloadHelper>();
            Bind<ITranslator>().ToConstant(FrontendSettingsViaduc.Instance);
            Bind<IUsageAnalyzer>().ToConstant(new UsageAnalyzer(GetUsageSettings(), UsageType.Download)).InSingletonScope();
            Bind<IUserDataAccess>().To<UserDataAccess>().InRequestScope().WithConstructorArgument(connectionString);
            Bind<ICacheHelper>().To<CacheHelper>().WithConstructorArgument("sftpLicenseKey", sftpLicensingKey);
            Bind<IUserAccessProvider>().ToMethod(context =>
            {
                var userDataAccess = context.Kernel.Get<IUserDataAccess>();

                return new UserAccessProvider(userDataAccess);
            });
            Bind<IAuthenticationHelper>().To<AuthenticationHelper>();

            Bind<IOrderDataAccess>().To<OrderDataAccess>().InRequestScope().WithConstructorArgument(connectionString);
            Bind<IApplicationRoleDataAccess>().To<ApplicationRoleDataAccess>().InRequestScope().WithConstructorArgument(connectionString);
            Bind<IApplicationRoleUserDataAccess>().To<ApplicationRoleUserDataAccess>().InRequestScope().WithConstructorArgument(connectionString);
            Bind<IDownloadTokenDataAccess>().To<DownloadTokenDataAccess>().InRequestScope().WithConstructorArgument(connectionString);
            Bind<IDownloadLogDataAccess>().To<DownloadLogDataAccess>().InRequestScope().WithConstructorArgument(connectionString);

            Bind<IRequestClient<DownloadAssetRequest, DownloadAssetResult>>()
                .ToMethod(BusConfig.RegisterDownloadAssetCallback);
            Bind<IRequestClient<GetAssetStatusRequest, GetAssetStatusResult>>()
                .ToMethod(BusConfig.RegisterGetAssetStatusCallback);
            Bind<IRequestClient<PrepareAssetRequest, PrepareAssetResult>>()
                .ToMethod(BusConfig.RegisterPrepareAssetCallback);
        }

        private static DownloadUsageSettings GetUsageSettings()
        {
            var settings = FrontendSettingsViaduc.Instance.GetServerSettings();
            var block = JsonHelper.FindTokenValue<JObject>(settings, "block");
            var usage = JsonHelper.GetByPath<JObject>(block, "download");
            var usageSettings = usage != null ? usage.ToObject<DownloadUsageSettings>() : new DownloadUsageSettings();

            usageSettings.Update();

            return usageSettings;
        }
    }
}