using CMI.Manager.Index;
using Topshelf;

namespace CMI.Host.Index
{
    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<IndexService>(s =>
                {
                    s.ConstructUsing(name => new IndexService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The index service updates data in Elasticsearch and also queries Elasticsearch");
                x.SetDisplayName("CMI Viaduc Index Service");
                x.SetServiceName("CMIIndexService");
            });
        }
    }
}