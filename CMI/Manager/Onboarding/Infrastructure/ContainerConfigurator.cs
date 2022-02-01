using System.Reflection;
using Autofac;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Utilities.Bus.Configuration;
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

            // register other Types here..
            
            return builder;
        }

        private static void RegisterBus(ContainerBuilder builder)
        {
            var helper = new ParameterBusHelper();
            BusConfigurator.ConfigureBus(builder, MonitoredServices.OnboardingService, (cfg, ctx) =>
            {
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });
        }
    }
}