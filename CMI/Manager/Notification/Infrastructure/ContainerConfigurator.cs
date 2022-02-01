using System.Reflection;
using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using MassTransit;

namespace CMI.Manager.Notification.Infrastructure
{
    internal class ContainerConfigurator
    {
        public static ContainerBuilder Configure()
        {
            var builder = new ContainerBuilder();

            // register the different consumers and classes
            builder.RegisterType<EmailMessage>().As<IEmailMessage>();
            builder.RegisterType<ParameterHelper>().As<IParameterHelper>();

            // just register all the consumers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();

            return builder;
        }
    }
}