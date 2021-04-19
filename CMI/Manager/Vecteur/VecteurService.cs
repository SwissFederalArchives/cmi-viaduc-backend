using System;
using System.Reflection;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Vecteur.Properties;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Microsoft.Owin.Hosting;
using Serilog;

namespace CMI.Manager.Vecteur
{
    public class VecteurService
    {
        private readonly IDisposable webApp;
        private SftpServer sftpServer;

        public VecteurService()
        {
            var url = VecteurSettings.Default.Address.Replace("{MachineName}", Environment.MachineName);
            webApp = WebApp.Start<Startup>(url);

            LogConfigurator.ConfigureForService();
        }

        public IBusControl Bus { get; private set; }

        public void Start()
        {
            Log.Information("Configuring Vecteur Service");

            StartBus();

            ApiKeyChecker.Key = VecteurSettings.Default.ApiKey;
            sftpServer = new SftpServer();
            sftpServer.Start();
            Log.Information("Vecteur service started");


            Startup.Kernel.Bind<IBus>().ToMethod(context => Bus).InSingletonScope();
            Startup.Kernel.Bind<IBusControl>().ToMethod(context => Bus).InSingletonScope();
        }

        private void StartBus()
        {
            var helper = new ParameterBusHelper();
            Bus = BusConfigurator.ConfigureBus(MonitoredServices.VecteurService,
                (cfg, host) => { helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg, host); });
            Bus.Start();
        }

        public void Stop()
        {
            Log.Information("Vecteur service is stopping");
            sftpServer.Stop();
            Bus.Stop();
            webApp.Dispose();
            Log.Information("Vecteur service stopped");
            Log.CloseAndFlush();
        }
    }
}