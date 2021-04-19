using CMI.Access.Repository;
using CMI.Contract.Parameter;
using CMI.Contract.Repository;
using CMI.Engine.PackageMetadata;
using MassTransit;
using Ninject;
using Ninject.Extensions.Conventions;

namespace CMI.Manager.Repository.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC Container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static StandardKernel Configure()
        {
            var kernel = new StandardKernel();

            // register the different consumers and classes
            kernel.Bind<IRepositoryManager>().To(typeof(RepositoryManager));
            kernel.Bind<IRepositoryConnectionFactory>().To(typeof(RepositoryConnectionFactory));
            kernel.Bind<IRepositoryDataAccess>().To(typeof(RepositoryDataAccess));
            kernel.Bind<IPackageValidator>().To(typeof(PackageValidator));
            kernel.Bind<IMetadataDataAccess>().To(typeof(MetadataDataAccess));
            kernel.Bind<IPackageHandler>().To(typeof(PackageHandler));
            kernel.Bind<IParameterHelper>().To(typeof(ParameterHelper));

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