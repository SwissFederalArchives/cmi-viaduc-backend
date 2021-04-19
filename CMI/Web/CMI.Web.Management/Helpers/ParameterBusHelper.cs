using System.Reflection;
using CMI.Contract.Monitoring;
using CMI.Utilities.Bus.Configuration;
using MassTransit;

namespace CMI.Web.Management.Helpers
{
    public static class ParameterBusHelper
    {
        static ParameterBusHelper()
        {
            var helper = new Contract.Parameter.ParameterBusHelper();
            ParameterBus = BusConfigurator.ConfigureBus(MonitoredServices.NotMonitored,
                (cfg, host) => { helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg, host); });

            ParameterBus.Start();
        }

        public static IBusControl ParameterBus { get; }
    }
}