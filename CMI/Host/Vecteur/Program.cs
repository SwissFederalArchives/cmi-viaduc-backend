using CMI.Manager.Vecteur;
using Topshelf;

namespace CMI.Host.Vecteur
{
    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<VecteurService>(s =>
                {
                    s.ConstructUsing(name => new VecteurService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The vecteur service offers an API for Vecteur");
                x.SetDisplayName("CMI Viaduc Vecteur Service");
                x.SetServiceName("CMIVecteurService");
            });
        }
    }
}