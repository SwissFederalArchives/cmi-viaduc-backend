using CMI.Manager.Viaduc;
using Topshelf;

namespace CMI.Host.Viaduc
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<ViaducService>(s =>
                {
                    s.ConstructUsing(name => new ViaducService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The Viaduc service allows Access to the Viaduc database.");
                x.SetDisplayName("CMI Viaduc Service");
                x.SetServiceName("CMIViaducService");
            });
        }
    }
}