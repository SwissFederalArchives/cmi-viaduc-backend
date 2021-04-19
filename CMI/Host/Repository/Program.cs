using CMI.Manager.Repository;
using Topshelf;

namespace CMI.Host.Repository
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<RepositoryService>(s =>
                {
                    s.ConstructUsing(name => new RepositoryService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The repository service gets zip packages from the digital repository.");
                x.SetDisplayName("CMI Viaduc Repository Service");
                x.SetServiceName("CMIRepositoryService");
            });
        }
    }
}