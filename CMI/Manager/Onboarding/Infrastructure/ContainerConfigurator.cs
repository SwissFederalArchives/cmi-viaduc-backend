using System.Reflection;
using Autofac;
using CMI.Access.Onboarding;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Manager.Onboarding.Properties;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Template;
using MassTransit;

namespace CMI.Manager.Onboarding.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static ContainerBuilder Configure()
        {
            var builder = new ContainerBuilder();
            
            RegisterBus(builder);
            builder.RegisterType<ConnectorSettings>().As<IConnectorSettings>();
            builder.RegisterType<OnboardingConnector>().As<IOnboardingConnector>();
            builder.RegisterType<UserDataAccess>().As<IUserDataAccess>().WithParameter("connectionString", DbConnectionSetting.Default.ConnectionString);
            builder.RegisterType<OnboardingManager>().As<IOnboardingManager>();
            builder.RegisterType<ParameterHelper>().As<IParameterHelper>();
            builder.RegisterType<MailHelper>().As<IMailHelper>();
            builder.RegisterType<DataBuilder>().As<IDataBuilder>();

            // SimpleConsumers
            builder.RegisterType(typeof(SimpleConsumer<StartOnboardingProcessRequest, StartOnboardingProcessResponse, IOnboardingManager>)).As(typeof(IConsumer<StartOnboardingProcessRequest>));
            builder.RegisterType(typeof(SimpleConsumer<HandleOnboardingCallbackRequest, HandleOnboardingCallbackResponse, IOnboardingManager>)).As(typeof(IConsumer<HandleOnboardingCallbackRequest>));
            return builder;
        }

        private static void RegisterBus(ContainerBuilder builder)
        {
            var helper = new ParameterBusHelper();
            BusConfigurator.ConfigureBus(builder, MonitoredServices.OnboardingService, (cfg, ctx) =>
            {
                cfg.UseNewtonsoftJsonSerializer();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });
        }
    }
}