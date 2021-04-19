using CMI.Manager.Asset;
using Topshelf;

namespace CMI.Host.Asset
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<AssetService>(s =>
                {
                    s.ConstructUsing(name => new AssetService());
                    s.WhenStarted(async tc => await tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The asset service works on zip packages from the digital repository.");
                x.SetDisplayName("CMI Viaduc Asset Service");
                x.SetServiceName("CMIAssetService");
            });
        }
    }
}