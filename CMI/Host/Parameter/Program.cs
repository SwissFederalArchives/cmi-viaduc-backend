using CMI.Manager.Parameter;
using Topshelf;

namespace CMI.Host.Parameter
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<ParameterService>(s =>
                {
                    s.ConstructUsing(name => new ParameterService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The parameter service helps makeing all parameters accessible for the management client.");
                x.SetDisplayName("CMI Viaduc Parameter Service");
                x.SetServiceName("CMIParameterService");
            });
        }
    }
}