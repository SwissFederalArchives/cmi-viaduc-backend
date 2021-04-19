using CMI.Manager.DataFeed;
using Topshelf;

namespace CMI.Host.DataFeed
{
    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<DataFeedService>(s =>
                {
                    s.ConstructUsing(name => new DataFeedService());
                    s.WhenStarted(async tc => await tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The data feed service fetches pending mutations from the AIS and pushes the data to the bus.");
                x.SetDisplayName("CMI Viaduc DataFeed Service");
                x.SetServiceName("CMIDataFeedService");
            });
        }
    }
}