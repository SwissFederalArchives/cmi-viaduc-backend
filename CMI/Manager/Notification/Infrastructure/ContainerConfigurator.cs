using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using MassTransit;
using Ninject;
using Ninject.Extensions.Conventions;

namespace CMI.Manager.Notification.Infrastructure
{
    internal class ContainerConfigurator
    {
        public static StandardKernel Configure()
        {
            var kernel = new StandardKernel();

            // register the different consumers and classes
            kernel.Bind<IEmailMessage>().To(typeof(EmailMessage));
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