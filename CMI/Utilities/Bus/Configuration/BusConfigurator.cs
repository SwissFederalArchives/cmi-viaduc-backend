using System;
using System.Linq;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Utilities.Bus.Configuration.Properties;
using GreenPipes;
using GreenPipes.Configurators;
using MassTransit;
using MassTransit.RabbitMqTransport;
using Serilog;

namespace CMI.Utilities.Bus.Configuration
{
    public static class BusConfigurator
    {
        public static string UserName => Settings.Default.RabbitMqUserName;
        public static string Password => Settings.Default.RabbitMqPassword;
        public static string Uri => Settings.Default.RabbitMqUri;

        /// <summary>
        ///     Gets the response URI.
        ///     Usually the response uri ist the same as the default uri.
        ///     But in the BAR environment the address for RabbitMq in the BV zone is different from SSZ zone.
        ///     So here we have the ability, to specify a different address
        /// </summary>
        /// <value>The response URI.</value>
        public static string ResponseUri => string.IsNullOrEmpty(Settings.Default.RabbitMqUriResponseAddress)
            ? Settings.Default.RabbitMqUri
            : Settings.Default.RabbitMqUriResponseAddress;

        public static IBusControl ConfigureBus(MonitoredServices monitoringName = MonitoredServices.NotMonitored,
            Action<IRabbitMqBusFactoryConfigurator, IRabbitMqHost> registrationAction = null)
        {
            Log.Information("Configuring Bus uri={uri} user={user}", Settings.Default.RabbitMqUri, Settings.Default.RabbitMqUserName);

            return MassTransit.Bus.Factory.CreateUsingRabbitMq(cfg =>
            {
                var host = cfg.Host(new Uri(Settings.Default.RabbitMqUri), hst =>
                {
                    hst.Username(Settings.Default.RabbitMqUserName);
                    hst.Password(Settings.Default.RabbitMqPassword);
                });
                if (monitoringName != MonitoredServices.NotMonitored)
                {
                    cfg.ReceiveEndpoint(host, string.Format(BusConstants.MonitoringServiceHeartbeatRequestQueue, monitoringName.ToString()),
                        ec => { ec.Consumer(() => new HeartbeatConsumer(monitoringName.ToString())); });
                }

                registrationAction?.Invoke(cfg, host);
            });
        }

        public static void ConfigureDefaultRetryPolicy(IRetryConfigurator retryPolicy)
        {
            retryPolicy.Exponential(10, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5), TimeSpan.FromSeconds(5));
        }

        public static void ChangeResponseAddress<T>(SendContext<T> context) where T : class
        {
            Log.Debug("ChangeResponseAddress for type of {TypeName} was called. ResponseAdress was {ResponseAddress}", typeof(T),
                context.ResponseAddress);
            Log.Debug("Current RabbitMqSettings Uri: {RabbitMqUri}, ResponseUri: {ResponseUri}", Uri, ResponseUri);

            if (!string.IsNullOrEmpty(ResponseUri))
            {
                var responseAddressUri = new Uri(ResponseUri);
                var defaultAddressUri = new Uri(Uri);

                // Only change response address, if a different response URI was configured
                if (!defaultAddressUri.Host.Equals(responseAddressUri.Host, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Build the response uri, only replance the host element as the rest should remain the same.
                    // The host property within the URI is always lowercase
                    var builder = new UriBuilder(context.ResponseAddress.Scheme, responseAddressUri.Host, context.ResponseAddress.Port,
                        context.ResponseAddress.AbsolutePath, context.ResponseAddress.Query);
                    context.ResponseAddress = builder.Uri;
                    Log.Information("ChangeResponseAddress for type of {TypeName} was called. ResponseAdress is now {ResponseAddress}", typeof(T),
                        context.ResponseAddress);
                }
            }
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