using CMI.Manager.Onboarding;
using Topshelf;

namespace CMI.Host.Onboarding
{
    internal class Program
    {
        // ReSharper disable once UnusedParameter.Local
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<OnboardingService>(s =>
                {
                    s.ConstructUsing(name => new OnboardingService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The service offers digital onboarding with Swisscom");
                x.SetDisplayName("CMI Viaduc Onboarding Service");
                x.SetServiceName("CMIOnboardingService");
            });
        }
    }
}