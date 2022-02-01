using System.Reflection;
using Autofac;
using CMI.Access.Harvest;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Contract.Harvest;
using MassTransit;
namespace CMI.Manager.ExternalContent.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static ContainerBuilder Configure()
        {
            var builder = new ContainerBuilder();

            // register the different consumers and classes
            builder.RegisterType<ExternalContentManager>().As<IExternalContentManager>().As<IReportExternalContentManager>();
            builder.RegisterType<LanguageSettings>().AsSelf();
            builder.RegisterType<ApplicationSettings>().AsSelf();
            builder.RegisterType<CachedLookupData>().AsSelf();
            builder.RegisterType<SipDateBuilder>().AsSelf();
            builder.RegisterType<DigitizationOrderBuilder>().AsSelf();
            builder.RegisterType<ArchiveRecordBuilder>().AsSelf();
            builder.RegisterType<AISDataAccess>().As<IDbExternalContentAccess>();
            builder.RegisterType<AISDataProvider>().As<IAISDataProvider>();

            // register all the consumers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();

            return builder;
        }
    }
}