using CMI.Access.Harvest;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Contract.Harvest;
using MassTransit;
using Ninject;
using Ninject.Extensions.Conventions;

namespace CMI.Manager.ExternalContent.Infrastructure
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
            kernel.Bind<IExternalContentManager>().To(typeof(ExternalContentManager));
            kernel.Bind<IDbDigitizationOrderAccess>().To(typeof(AISDataAccess));
            kernel.Bind<IAISDataProvider>().To(typeof(AISDataProvider));


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