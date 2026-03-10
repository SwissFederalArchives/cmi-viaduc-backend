using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Manager.ExternalContent.Consumers;
using CMI.Manager.ExternalContent.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.Reflection;
using System.Runtime.Remoting.Contexts;

namespace CMI.Manager.ExternalContent
{
    /// <summary>
    ///     The ExternalContentService is configuring the bus and IoC container for the external content manager.
    /// </summary>
    public class ExternalContentService
    {
        private IServiceCollection services;
        private IBusControl bus;

        public ExternalContentService()
        {
            // Configure IoC Container
            services = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        /// <summary>
        ///     Starts the ExternalContent Service.
        ///     Called by the service host when the service is started.
        /// </summary>
        public void Start()
        {
            Log.Information("ExternalContent service is starting");

            // Configure Bus
            BusConfigurator.ConfigureBusModern(services, MonitoredServices.ExternalContentService, AddConsumers, (context, cfg) =>
            {
                cfg.ReceiveEndpoint(BusConstants.ManagementApiGetDigitizationOrderData,
                    ec => { ec.ConfigureConsumer<DigitizationOrderConsumer>(context); });
                cfg.UseNewtonsoftJsonSerializer();
            });

            var provider = services.BuildServiceProvider();
            bus = provider.GetRequiredService<IBusControl>();
            bus.Start();

            Log.Information("ExternalContent service started");
        }

        private void AddConsumers(IBusRegistrationConfigurator x)
        {
            // registers all IConsumer implementations in this assembly
            x.AddConsumers(Assembly.GetExecutingAssembly());
        }

        /// <summary>
        ///     Stops the ExternalContent Service.
        ///     Called by the service host when the service is stopped.
        /// </summary>
        public void Stop()
        {
            Log.Information("ExternalContent service is stopping");
            bus.Stop();
            Log.Information("ExternalContent service stopped");
            Log.CloseAndFlush();
        }
    }
}