using Topshelf;

namespace CMI.Tools.AnonymizeServiceMock
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<AnonymizeService>(s =>
                {
                    s.ConstructUsing(name => new AnonymizeService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("A service to have text blackened. The service anonymises names of natural and legal persons. " +
                                 "Furthermore, other personal identification features such as date of birth or AHV numbers are recognised.");
                x.SetDisplayName("CMI Viaduc Anonymize Service");
                x.SetServiceName("CMIAnonymizeService");
            });
        }
    }
}
