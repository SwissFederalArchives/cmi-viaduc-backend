using System;
using Autofac;
using CMI.Manager.Vecteur.Properties;
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


        }

        private void StartBus()
        {
            Bus = Startup.Container.Resolve<IBusControl>();
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