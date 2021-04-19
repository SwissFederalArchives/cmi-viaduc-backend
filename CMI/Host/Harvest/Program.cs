using CMI.Manager.Harvest;
using Topshelf;

namespace CMI.Host.Harvest
{
    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<HarvestService>(s =>
                {
                    s.ConstructUsing(name => new HarvestService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The harvest service gets pending sync operation from the bus and gathers the data from the AIS.");
                x.SetDisplayName("CMI Viaduc Harvest Service");
                x.SetServiceName("CMIHarvestService");
            });
        }
    }
}