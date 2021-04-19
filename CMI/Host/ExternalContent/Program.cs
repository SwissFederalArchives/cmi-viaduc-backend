using CMI.Manager.ExternalContent;
using Topshelf;

namespace CMI.Host.ExternalContent
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<ExternalContentService>(s =>
                {
                    s.ConstructUsing(name => new ExternalContentService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The external content service provides data from external systems.");
                x.SetDisplayName("CMI Viaduc External Content Service");
                x.SetServiceName("CMIExternalContentService");
            });
        }
    }
}