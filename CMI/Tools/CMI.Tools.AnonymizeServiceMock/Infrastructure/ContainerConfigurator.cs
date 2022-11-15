using System.Reflection;
using Autofac;

namespace CMI.Tools.AnonymizeServiceMock.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static ContainerBuilder Configure()
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<WordList>().SingleInstance();

            // register other Types here..
         
            return builder;
        }
    }
}