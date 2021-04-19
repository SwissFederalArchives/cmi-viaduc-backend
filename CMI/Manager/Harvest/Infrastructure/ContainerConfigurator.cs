using CMI.Access.Harvest;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Contract.Harvest;
using CMI.Contract.Parameter;
using MassTransit;
using Ninject;
using Ninject.Extensions.Conventions;

namespace CMI.Manager.Harvest.Infrastructure
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
            kernel.Bind<IHarvestManager>().To(typeof(HarvestManager));
            kernel.Bind<IDbMetadataAccess>().To(typeof(AISDataAccess));
            kernel.Bind<IDbMutationQueueAccess>().To(typeof(AISDataAccess));
            kernel.Bind<IDbResyncAccess>().To(typeof(AISDataAccess));
            kernel.Bind<IDbStatusAccess>().To(typeof(AISDataAccess));
            kernel.Bind<IDbTestAccess>().To(typeof(AISDataAccess));
            kernel.Bind<IAISDataProvider>().To(typeof(AISDataProvider));
            kernel.Bind<CachedLookupData>().ToSelf().InSingletonScope();
            kernel.Bind<IParameterHelper>().To<ParameterHelper>();
            kernel.Bind<ICachedHarvesterSetting>().To<CachedHarvesterSetting>().InSingletonScope();

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