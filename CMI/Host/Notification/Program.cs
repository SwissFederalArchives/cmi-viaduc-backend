using CMI.Manager.Notification;
using Topshelf;

namespace CMI.Host.Notification
{
    internal class Program
    {
        private static void Main()
        {
            HostFactory.Run(x =>
            {
                x.Service<NotificationService>(s =>
                {
                    s.ConstructUsing(name => new NotificationService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The notification service sends emails.");
                x.SetDisplayName("CMI Viaduc Notification Service");
                x.SetServiceName("CMINotificationService");
            });
        }
    }
}