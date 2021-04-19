using System;
using CMI.Access.Common;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Manager.Order.Properties;
using CMI.Utilities.Cache.Access;
using CMI.Utilities.Template;
using MassTransit;
using Ninject;
using Ninject.Extensions.Conventions;

namespace CMI.Manager.Order.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static StandardKernel Configure()
        {
            var kernel = new StandardKernel();

            // register the different consumers and classes
            kernel.Bind<ISearchIndexDataAccess>().To<SearchIndexDataAccess>()
                .WithConstructorArgument("address", arg => new Uri(ElasticConnectionSetting.Default.ConnectionString));
            kernel.Bind<IPublicOrder>().To<OrderManager>();
            kernel.Bind<IOrderDataAccess>().To<OrderDataAccess>().WithConstructorArgument(arg => DbConnectionSetting.Default.ConnectionString);
            kernel.Bind<IUserDataAccess>().To<UserDataAccess>().WithConstructorArgument(arg => DbConnectionSetting.Default.ConnectionString);
            kernel.Bind<ICacheHelper>().To(typeof(CacheHelper)).WithConstructorArgument("sftpLicenseKey", Settings.Default.SftpLicenseKey);
            kernel.Bind<StatusWechsler>().ToSelf();
            kernel.Bind<IParameterHelper>().To<ParameterHelper>();
            kernel.Bind<IMailHelper>().To<MailHelper>();
            kernel.Bind<IDataBuilder>().To<DataBuilder>();

            // just register all the consumers using Ninject.Extensions.Conventions
            kernel.Bind(x =>
            {
                x.FromThisAssembly()
                    .SelectAllClasses()
                    .InheritedFrom<IConsumer>()
                    .BindToSelf();
            });


            return kernel;
        }
    }
}