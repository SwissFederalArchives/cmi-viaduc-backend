using CMI.Access.Sql.Viaduc;
using CMI.Access.Sql.Viaduc.AblieferndeStellen;
using CMI.Access.Sql.Viaduc.File;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Utilities.ProxyClients.Order;
using CMI.Utilities.Template;
using CMI.Web.Common.Auth;
using CMI.Web.Common.Helpers;
using CMI.Web.Management.api.Configuration;
using Ninject.Modules;
using Ninject.Web.Common;

namespace CMI.Web.Management.DependencyInjection
{
    public class ManagementModules : NinjectModule
    {
        /// <summary>Loads the module into the kernel.</summary>
        /// <devdoc>
        ///     Der Scope wird auf .InRequestScope() festgelegt, damit pro Request pro Klasse exakt eine neue Instanz pro Klasse
        ///     erstellt wird.
        /// </devdoc>
        public override void Load()
        {
            var connectionString = ManagementSettingsViaduc.Instance.SqlConnectionString;

            Bind<IUserDataAccess>().To<UserDataAccess>().InRequestScope().WithConstructorArgument(connectionString);

            Bind<IApplicationRoleDataAccess>().To<ApplicationRoleDataAccess>().InRequestScope().WithConstructorArgument(connectionString);
            Bind<IApplicationRoleUserDataAccess>().To<ApplicationRoleUserDataAccess>().InRequestScope().WithConstructorArgument(connectionString);

            Bind<IAblieferndeStelleDataAccess>().To<AblieferndeStelleDataAccess>().InRequestScope().WithConstructorArgument(connectionString);
            Bind<IAblieferndeStelleTokenDataAccess>().To<AblieferndeStelleTokenDataAccess>().InRequestScope()
                .WithConstructorArgument(connectionString);
            Bind<IDownloadTokenDataAccess>().To<DownloadTokenDataAccess>().InRequestScope().WithConstructorArgument(connectionString);
            Bind<IAuthenticationHelper>().To<AuthenticationHelper>();


            Bind<IParameterHelper>().To<ParameterHelper>();
            Bind<IMailHelper>().To<MailHelper>();
            Bind<NewsDataAccess>().To<NewsDataAccess>().InRequestScope().WithConstructorArgument(connectionString);
            Bind<ICmiSettings>().To<CmiSettings>();
            Bind<IWebCmiConfigProvider>().To<WebCmiConfigProvider>();

            Bind<IFileDownloadHelper>().To<FileDownloadHelper>();
            Bind<IPublicOrder>().To<OrderManagerClient>();
        }
    }
}