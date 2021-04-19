using System;
using System.Configuration;
using System.Reflection;
using System.Threading;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Manager.Onboarding.Properties;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Logging.Configurator;
using MassTransit;
using Microsoft.Owin.Hosting;
using Ninject;
using Serilog;

namespace CMI.Manager.Onboarding
{
    public class OnboardingService
    {
        private readonly UserDataAccess dataAccess;
        private IBusControl bus;
        private ProcessSftp processSftp;
        private Timer timerDeleteOld;
        private Timer timerProcessFiles;
        private IDisposable webApp;


        public OnboardingService()
        {
            Startup.Kernel = new StandardKernel();
            Startup.Kernel.Load(Assembly.GetExecutingAssembly());

            LogConfigurator.ConfigureForService();

            dataAccess = new UserDataAccess(DbConnectionSetting.Default.ConnectionString);
        }

        public void Start()
        {
            Log.Information("Onboarding service is starting");

            InitBus();


            webApp = WebApp.Start<Startup>(OnboardingPostbackSetting.Default.PostbackBaseUrl);

            processSftp = new ProcessSftp(bus);
            timerProcessFiles = new Timer(ProcessFiles, null, 0, Timeout.Infinite);

            timerDeleteOld = new Timer(DeleteOldRejectionReasons, null, TimeSpan.FromHours(3), TimeSpan.FromHours(3));

            Log.Information("Onboarding service started");
        }


        public void Stop()
        {
            Log.Information("Onboarding service is stopping");

            webApp.Dispose();
            timerProcessFiles.Dispose();
            timerDeleteOld.Dispose();

            Log.Information("Onboarding service stopped");
            Log.CloseAndFlush();
        }


        private void ProcessFiles(object state)
        {
            try
            {
                processSftp.ProcessAll();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception on process files from SFTP");
            }

            timerProcessFiles.Change(120000, Timeout.Infinite); // Alle 2 Minuten
        }

        private void DeleteOldRejectionReasons(object state)
        {
            try
            {
                Log.Information("Delete old rejection reasons");
                dataAccess.DeleteOldRejectionReasons();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception on delete old rejection reasons");
            }
        }

        private void InitBus()
        {
            var helper = new ParameterBusHelper();
            bus = BusConfigurator.ConfigureBus(MonitoredServices.OnboardingService, (cfg, host) =>
            {
                helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg, host);

                cfg.UseSerilog();
            });

            bus.Start();

            // Add the bus instance to the IoC container            
            Startup.Kernel.Bind<IBus>().ToMethod(context => bus).InSingletonScope();
        }
    }
}