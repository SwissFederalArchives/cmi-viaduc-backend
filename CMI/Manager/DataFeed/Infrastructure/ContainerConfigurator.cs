using CMI.Access.Harvest;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Contract.Harvest;
using MassTransit;
using Ninject;
using Ninject.Extensions.Conventions;

namespace CMI.Manager.DataFeed.Infrastructure
{
    internal class ContainerConfigurator
    {
        public static StandardKernel Configure()
        {
            var kernel = new StandardKernel();

            // register the different consumers and classes
            kernel.Bind<IDbMutationQueueAccess>().To(typeof(AISDataAccess));
            kernel.Bind<CheckMutationQueueJob>().ToSelf();
            kernel.Bind<IAISDataProvider>().To(typeof(AISDataProvider));
            kernel.Bind<ICancelToken>().ToMethod(context => new JobCancelToken()).InSingletonScope();

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