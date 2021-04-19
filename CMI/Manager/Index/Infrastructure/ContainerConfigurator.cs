using System;
using System.IO;
using CMI.Access.Common;
using CMI.Manager.Index.Config;
using MassTransit;
using Ninject;
using Ninject.Extensions.Conventions;

namespace CMI.Manager.Index.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC Container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static StandardKernel Configure()
        {
            var kernel = new StandardKernel();

            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "customFieldsConfig.json");

            // register the different consumers and classes
            kernel.Bind<IIndexManager>().To(typeof(IndexManager));
            kernel.Bind<ISearchIndexDataAccess>().To(typeof(SearchIndexDataAccess));
            kernel.Bind<ITestSearchIndexDataAccess>().To(typeof(SearchIndexDataAccess));
            kernel.Bind<IElasticLogManager>().To(typeof(ElasticLogManager));
            kernel.Bind<CustomFieldsConfiguration>().To<CustomFieldsConfiguration>().InSingletonScope().WithConstructorArgument(configFile);

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