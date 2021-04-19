using MassTransit;
using Ninject;
using Ninject.Extensions.Conventions;

namespace CMI.Manager.Monitoring.Infrastructure
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