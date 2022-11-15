using System.Reflection;
using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Onboarding.Infrastructure;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;

namespace CMI.Manager.Onboarding
{
    public class OnboardingService
    {
        private readonly ContainerBuilder containerBuilder;
        private IBusControl bus;

        public OnboardingService()
        {
            containerBuilder = ContainerConfigurator.Configure();
            LogConfigurator.ConfigureForService();
        }

        public void Start()
        {
            Log.Information("Onboarding service is starting");

            var helper = new ParameterBusHelper();
            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.OnboardingService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(string.Format(BusConstants.OnboardingManagerStartProcessMessageQueue, nameof(StartOnboardingProcessRequest)),
                    ec =>
                    {
                        ec.Consumer(ctx.Resolve<IConsumer<StartOnboardingProcessRequest>>);
                    });
                cfg.ReceiveEndpoint(string.Format(BusConstants.OnboardingManagerHandleCallbackMessageQueue, nameof(HandleOnboardingCallbackRequest)),
                    ec =>
                    {
                        ec.Consumer(ctx.Resolve<IConsumer<HandleOnboardingCallbackRequest>>);
                    });

                cfg.UseNewtonsoftJsonSerializer();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });

            var container = containerBuilder.Build();
            bus = container.Resolve<IBusControl>();
            bus.Start();

            Log.Information("Onboarding service started");
        }

        public void Stop()
        {
            Log.Information("Onboarding service is stopping");
            bus.Stop();
            Log.Information("Onboarding service stopped");
            Log.CloseAndFlush();
        }
    }
}