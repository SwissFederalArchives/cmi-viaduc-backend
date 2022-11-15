using System.Reflection;
using Autofac;
using CMI.Contract.Monitoring;
using CMI.Contract.Order;
using CMI.Contract.Parameter;
using CMI.Engine.MailTemplate;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.ProxyClients.Order;
using CMI.Utilities.Template;
using MassTransit;

namespace CMI.Manager.Vecteur.Infrastructure
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

            // register other Types here..
            builder.RegisterType<MailHelper>().As<IMailHelper>();
            builder.RegisterType<ParameterHelper>().As<IParameterHelper>();
            builder.RegisterType<DataBuilder>().As<IDataBuilder>();
            builder.RegisterType<OrderManagerClient>().As<IPublicOrder>();
            builder.RegisterType<VecteurActionsClient>().As<IVecteurActions>();
            builder.RegisterType<DigitizationHelper>().As<IDigitizationHelper>();
            builder.RegisterType<MessageBusCallHelper>().As<IMessageBusCallHelper>();
            return builder;
        }

        private static void RegisterBus(ContainerBuilder builder)
        {
            var helper = new ParameterBusHelper();
            BusConfigurator.ConfigureBus(builder, MonitoredServices.VecteurService,
                (cfg, ctx) =>
                {
                    cfg.UseNewtonsoftJsonSerializer();
                    helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
                });
        }
    }
}