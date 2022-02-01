using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CMI.Contract.Parameter.GetParameter;
using CMI.Contract.Parameter.SaveParameter;
using MassTransit;
using MassTransit.RabbitMqTransport;

namespace CMI.Contract.Parameter
{
    public class ParameterBusHelper
    {
        public void SubscribeAllSettingsInAssembly(Assembly assembly, IRabbitMqBusFactoryConfigurator cfg)
        {
            var method = GetType().GetMethod(nameof(SubscribeAllParameterEvents));
            foreach (var type in assembly.GetExportedTypes().Where(type => typeof(ISetting).IsAssignableFrom(type)))
            {
                var generic = method?.MakeGenericMethod(type);
                generic?.Invoke(this, new object[] {cfg});
                Debug.WriteLine(type.FullName + " subscribed");
            }
        }

        /// <summary>
        ///     Diese Methode muss public sein, damit der Code funktioniert
        /// </summary>
        public void SubscribeAllParameterEvents<T>(IRabbitMqBusFactoryConfigurator cfg) where T : ISetting
        {
            var getQueueName = "p.parameter.getParameterEventHandler." + typeof(T).FullName;
            cfg.ReceiveEndpoint(getQueueName, ep => { ep.Consumer<GetParameterEventConsumer<T>>(); });
            var saveQueueName = "p.parameter.saveParameterEventHandler." + typeof(T).FullName;
            cfg.ReceiveEndpoint(saveQueueName, ep => { ep.Consumer<SaveParameterEventConsumer<T>>(); });
        }
    }
}