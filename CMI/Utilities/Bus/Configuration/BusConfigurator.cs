using System;
using System.Linq;
using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Utilities.Bus.Configuration.Properties;
using MassTransit;
using Serilog;
using Serilog.Extensions.Logging;

namespace CMI.Utilities.Bus.Configuration
{
    public static class BusConfigurator
    {
        public static string UserName => Settings.Default.RabbitMqUserName;
        public static string Password => Settings.Default.RabbitMqPassword;
        public static string Uri => Settings.Default.RabbitMqUri;

        public static void ConfigureBus(ContainerBuilder builder, MonitoredServices monitoringName = MonitoredServices.NotMonitored, Action<IRabbitMqBusFactoryConfigurator, IComponentContext> registrationAction = null)
        {
            Log.Information("Configuring Bus uri={uri} user={user}", Settings.Default.RabbitMqUri, Settings.Default.RabbitMqUserName);

            // Configure Logging for Masstransit using Serilog 
            LogContext.ConfigureCurrentLogContext(new SerilogLoggerFactory(Log.Logger));

            builder.Register(ctx =>
                {
                    return MassTransit.Bus.Factory.CreateUsingRabbitMq(cfg =>
                    {
                        cfg.Host(new Uri(Settings.Default.RabbitMqUri), hst =>
                        {
                            hst.Username(Settings.Default.RabbitMqUserName);
                            hst.Password(Settings.Default.RabbitMqPassword);
                        });
                        if (monitoringName != MonitoredServices.NotMonitored)
                        {
                            cfg.ReceiveEndpoint(string.Format(BusConstants.MonitoringServiceHeartbeatRequestQueue, monitoringName.ToString()),
                                ec => { ec.Consumer(() => new HeartbeatConsumer(monitoringName.ToString())); });
                        }


                        var context = ctx.Resolve<IComponentContext>();
                        registrationAction?.Invoke(cfg, context);
                    });
            })
            .SingleInstance()
            .As<IBusControl>()
            .As<IBus>()
            .ExternallyOwned();
        }

        public static void ConfigureDefaultRetryPolicy(IRetryConfigurator retryPolicy)
        {
            retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5));
        }


        /// <summary>
        ///     Sets the prefetch count for a specific endpoint if there is a setting.
        ///     If there is no setting found in the app.config file. No changes are made.
        /// </summary>
        /// <param name="ec">The endpoint to configure</param>
        public static void SetPrefetchCountForEndpoint(IReceiveEndpointConfigurator ec)
        {
            if (PrefetchCountSettings.Items.ContainsKey(ec.InputAddress.Segments.Last()))
            {
                ((IRabbitMqReceiveEndpointConfigurator) ec).PrefetchCount = PrefetchCountSettings.Items[ec.InputAddress.Segments.Last()];
            }
        }

        /// <summary>
        ///     Returns the PrefetchCount setting of the given queue name (endpoint)
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns></returns>
        public static ushort? GetPrefetchCountForEndpoint(string queueName)
        {
            if (PrefetchCountSettings.Items.ContainsKey(queueName))
            {
                return PrefetchCountSettings.Items[queueName];
            }

            return null;
        }
        
    }
}