using CMI.Manager.DocumentConverter;
using Topshelf;

namespace CMI.Host.DocumentConverter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<DocumentService>(s =>
                {
                    s.ConstructUsing(name => new DocumentService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The document converter service converts documents.");
                x.SetDisplayName("CMI Viaduc DocumentConverter Service");
                x.SetServiceName("CMIDocumentConverterService");
            });
        }
    }
}