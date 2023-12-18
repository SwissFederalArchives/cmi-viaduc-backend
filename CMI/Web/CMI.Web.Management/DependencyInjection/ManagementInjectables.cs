using System.Reflection;
using Autofac;
using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.AblieferndeStellen;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Access.Sql.Viaduc.EF.Helper;
using CMI.Access.Sql.Viaduc.File;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Utilities.Cache.Access;
using CMI.Utilities.ProxyClients.Order;
using CMI.Utilities.Template;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.api.Configuration;
using CMI.Web.Management.api.Data;
using MassTransit;


namespace CMI.Web.Management.DependencyInjection
{
    public static class ManagementInjectables
    {
        public static void RegisterManagementInjectables(this ContainerBuilder builder)
        {
            builder.RegisterType<OrderManagerClient>().AsSelf();
            builder.RegisterType<CollectionManagerClient>().As<ICollectionManager>();
            builder.RegisterType<ManuelleKorrekturManagerClient>().As<IManuelleKorrekturManager>();
            builder.RegisterType<ExcelExportHelper>().AsSelf();
            builder.RegisterType<CacheHelper>().As<ICacheHelper>().WithParameter("sftpLicenseKey", WebHelper.Settings["sftpLicenseKey"]);
            
            var connectionString = ManagementSettingsViaduc.Instance.SqlConnectionString;
            var connectionStringEF = ManagementSettingsViaduc.Instance.SqlConnectionStringEF;

            builder.RegisterType<ViaducDb>().AsSelf().WithParameter(nameof(connectionString), connectionStringEF);
            builder.RegisterType<AccessHelper>().AsSelf();
            builder.RegisterType<ManuelleKorrekturAccess>().As<IManuelleKorrekturAccess>();
            builder.RegisterType<UserDataAccess>().As<IUserDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<ApplicationRoleDataAccess>().As<IApplicationRoleDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<ApplicationRoleUserDataAccess>().As<IApplicationRoleUserDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<AblieferndeStelleDataAccess>().As<IAblieferndeStelleDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<AblieferndeStelleTokenDataAccess>().As<IAblieferndeStelleTokenDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<DownloadTokenDataAccess>().As<IDownloadTokenDataAccess>().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<NewsDataAccess>().AsSelf().InstancePerRequest().WithParameter(nameof(connectionString), connectionString);

            builder.Register(c => BusConfig.CreateGetElasticLogRecordsRequestClient()).As<IRequestClient<GetElasticLogRecordsRequest>>();
            builder.Register(c => BusConfig.RegisterDownloadAssetCallback()).As<IRequestClient<DownloadAssetRequest>>();
            builder.Register(c => BusConfig.CreateDoesExistInCacheClient()).As<IRequestClient<DoesExistInCacheRequest>>();

            builder.RegisterType<AuthenticationHelper>().As<IAuthenticationHelper>();
            builder.RegisterType<ParameterHelper>().As<IParameterHelper>();
            builder.RegisterType<MailHelper>().As<IMailHelper>();

            builder.RegisterType<CmiSettings>().As<ICmiSettings>();
            builder.RegisterType<WebCmiConfigProvider>().As<IWebCmiConfigProvider>();

            builder.RegisterType<OrderManagerClient>().As<IPublicOrder>();
            builder.RegisterType<DownloadLogHelper>().As<IDownloadLogHelper>();
            builder.RegisterType<AbbyyProgressInfo>().SingleInstance().AsSelf();

            // register all the consumers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();
        }
    }
}