using System.Reflection;
using Autofac;
using CMI.Access.Harvest;
using CMI.Access.Harvest.ScopeArchiv;
using CMI.Contract.Harvest;
using CMI.Contract.Parameter;
using MassTransit;

namespace CMI.Manager.Harvest.Infrastructure
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
            builder.RegisterType<LanguageSettings>().AsSelf();
            builder.RegisterType<ApplicationSettings>().AsSelf();
            builder.RegisterType<CachedLookupData>().AsSelf();
            builder.RegisterType<SipDateBuilder>().AsSelf();
            builder.RegisterType<DigitizationOrderBuilder>().AsSelf();
            builder.RegisterType<ArchiveRecordBuilder>().AsSelf();
            builder.RegisterType<HarvestManager>().As<IHarvestManager>();
            builder.RegisterType<AISDataAccess>().AsImplementedInterfaces();
            builder.RegisterType<AISDataProvider>().As<IAISDataProvider>();
            builder.RegisterType<ParameterHelper>().As<IParameterHelper>();
            builder.RegisterType<CachedHarvesterSetting>().As<ICachedHarvesterSetting>().SingleInstance();
            
            // register all the consumers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();

            return builder;
        }
    }
}