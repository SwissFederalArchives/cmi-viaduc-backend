using System;
using System.IO.Abstractions;
using System.Net.Mail;
using System.Reflection;
using Autofac;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Common;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Serilog;

namespace CMI.Manager.Cache
{
    public class CacheService
    {
        private readonly IParameterHelper parameterHelper;
        private CacheDeleter deleter;
        private SftpServer sftpServer;
        private CacheSizeWarner warner;


        public CacheService()
        {
            LogConfigurator.ConfigureForService();
            parameterHelper = new ParameterHelper();
        }


        public IBusControl Bus { get; private set; }

        public void Start()
        {
            Log.Information("Configuring Cache Service");

            StartBus();

            sftpServer = new SftpServer();
            sftpServer.Start();

            deleter = new CacheDeleter(parameterHelper, new FileSystem(), new Sleeper());
            deleter.Start();

            warner = new CacheSizeWarner(parameterHelper);
            warner.ThresholdExceeded += Warner_ThresholdExceeded;
            warner.Start();

            Log.Information("Cache service started");
        }


        private async void Warner_ThresholdExceeded(object sender, ThresholdExceededEventArgs e)
        {
            var ep = await Bus.GetSendEndpoint(new Uri(Bus.Address, BusConstants.NotificationManagerMessageQueue));

            var msg = new EmailMessage
            {
                Body = $@"
Der Cache-Speicher hat eine kritische Grösse erreicht: <br>
<br>
Maximal zulässiger Sollwert:	{e.Threshold}<br>
Istwert: 			            {e.CurrentSize}<br>
<br>
Vom AppO ist folgende Massnahme zu ergreifen:<br>
<br>
-	Schaffen von neuem Speicherplatz für den Cache (Kauf von neuem Speicherplatz) und entsprechende Erhöhung des Parameters WarningThresholdCacheSize (Vorgehen gemäss Anwenderhandbuch)",
                Subject = "Warnung Cache-Speicher",
                To = parameterHelper.GetSetting<CacheSettings>().MailRecipient,
                Priority = MailPriority.High
            };
            await ep.Send<IEmailMessage>(msg);
        }

        private void StartBus()
        {
            var helper = new ParameterBusHelper();
            var containerBuilder = new ContainerBuilder();

            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.CacheService, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(BusConstants.CacheDoesExistRequestQueue,
                    ec => { ec.Consumer(() => new DoesExistInCacheRequestConsumer()); });

                cfg.ReceiveEndpoint(BusConstants.CacheConnectionInfoRequestQueue,
                    ec => { ec.Consumer(() => new CacheConnectionInfoRequestConsumer()); });

                cfg.ReceiveEndpoint(BusConstants.CacheDeleteFile, ec =>
                {
                    ec.Consumer(() => new DeleteFileFromCacheConsumer());
                    ec.UseRetry(BusConfigurator.ConfigureDefaultRetryPolicy);
                });

                cfg.UseNewtonsoftJsonSerializer();
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg);
            });

            var container = containerBuilder.Build();
            Bus = container.Resolve<IBusControl>();
            Bus.Start();
        }

        public void Stop()
        {
            Log.Information("Cache service is stopping");
            sftpServer.Stop();
            Bus.Stop();
            Log.Information("Cache service stopped");
            Log.CloseAndFlush();
        }
    }
}