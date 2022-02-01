using System.Web.Mvc;
using Autofac;
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

namespace CMI.Web.Frontend.DependencyInjection
{
    public static class FrontendInjectables
    {
        public static void RegisterFrontendInjectables(this ContainerBuilder builder)
        {
            var connectionString = FrontendSettingsViaduc.Instance.SqlConnectionString;
            var sftpLicenseKey = WebHelper.Settings["sftpLicenseKey"];

            builder.RegisterType<ExcelExportHelper>().AsSelf();
            builder.RegisterType<VeExportRecordHelper>().AsSelf();
            builder.RegisterType<ParameterHelper>().As<IParameterHelper>();
            builder.Register(c => c.Resolve<IParameterHelper>().GetSetting<DigitalisierungsbeschraenkungSettings>()).AsSelf();
            builder.Register(c => c.Resolve<IParameterHelper>().GetSetting<ManagementClientSettings>()).AsSelf();
            builder.Register(c => c.Resolve<IParameterHelper>().GetSetting<VerwaltungsausleiheSettings>()).AsSelf();
            builder.Register(c => c.Resolve<IParameterHelper>().GetSetting<AutomatischeBenachrichtigungAnKontrollstelle>()).AsSelf();
            builder.Register(c => c.Resolve<IParameterHelper>().GetSetting<FrontendDynamicTextSettings>()).AsSelf();
            
            builder.Register(c => ServerSettings.GetServerSettings<ElasticSettings>()).As<IElasticSettings>();
            
            builder.RegisterType<EntityProvider>().As<IEntityProvider>().InstancePerRequest();
            builder.RegisterType<ElasticClientProvider>().As<IElasticClientProvider>();
            builder.RegisterType<ElasticService>().As<IElasticService>().SingleInstance().ExternallyOwned();
            builder.RegisterType<PhysicalFileSystem>().As<IFileSystem>().SingleInstance();

            builder.RegisterType<ModelData>().As<IModelData>();
            builder.RegisterType<CmiSettings>().As<ICmiSettings>();
            builder.RegisterType<WebCmiConfigProvider>().As<IWebCmiConfigProvider>();
            builder.RegisterType<KontrollstellenInformer>().As<IKontrollstellenInformer>();

            builder.RegisterType<OrderManagerClient>().As<IPublicOrder>();
            builder.RegisterType<FileDownloadHelper>().As<IFileDownloadHelper>();

            builder.Register(c => FrontendSettingsViaduc.Instance).As<ITranslator>().SingleInstance().ExternallyOwned();
            builder.Register(c => new UsageAnalyzer(GetUsageSettings(), UsageType.Download)).As<IUsageAnalyzer>().SingleInstance().ExternallyOwned();

            builder.RegisterType<UserDataAccess>().As<IUserDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<CacheHelper>().As<ICacheHelper>().WithParameter(nameof(sftpLicenseKey), sftpLicenseKey);

            builder.Register(c => new UserAccessProvider(c.Resolve<IUserDataAccess>())).As<IUserAccessProvider>();
            builder.RegisterType<AuthenticationHelper>().As<IAuthenticationHelper>();

            builder.RegisterType<OrderDataAccess>().As<IOrderDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<ApplicationRoleUserDataAccess>().As<IApplicationRoleUserDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<ApplicationRoleDataAccess>().As<IApplicationRoleDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<DownloadTokenDataAccess>().As<IDownloadTokenDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<DownloadLogDataAccess>().As<IDownloadLogDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);

            builder.Register(c => BusConfig.RegisterDownloadAssetCallback()).As<IRequestClient<DownloadAssetRequest>>();
            builder.Register(c => BusConfig.RegisterGetAssetStatusCallback()).As<IRequestClient<GetAssetStatusRequest>>();
            builder.Register(c => BusConfig.RegisterPrepareAssetCallback()).As<IRequestClient<PrepareAssetRequest>>();
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