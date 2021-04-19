using CMI.Manager.Monitoring;
using Topshelf;

namespace CMI.Host.Monitoring
{
    public class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<MonitoringService>(s =>
                {
                    s.ConstructUsing(name => new MonitoringService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The monitoring service checks the hearbeat of all services.");
                x.SetDisplayName("CMI Viaduc Monitoring Service");
                x.SetServiceName("CMIMonitoringService");
            });
        }
    }
}