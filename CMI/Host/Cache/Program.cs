using CMI.Manager.Cache;
using Topshelf;

namespace CMI.Host.Cache
{
    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<CacheService>(s =>
                {
                    s.ConstructUsing(name => new CacheService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The service stores files over a defined period of time.");
                x.SetDisplayName("CMI Viaduc Cache Service");
                x.SetServiceName("CMICacheService");
            });
        }
    }
}