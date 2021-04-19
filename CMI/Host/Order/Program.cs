using CMI.Manager.Order;
using Topshelf;

namespace CMI.Host.Order
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            HostFactory.Run(x =>
            {
                x.Service<OrderService>(s =>
                {
                    s.ConstructUsing(name => new OrderService());
                    s.WhenStarted(tc => tc.Start());
                    s.WhenStopped(tc => tc.Stop());
                });
                x.RunAsLocalSystem();

                x.SetDescription("The order service handles all kinds of orders.");
                x.SetDisplayName("CMI Viaduc Order Service");
                x.SetServiceName("CMIOrderService");
            });
        }
    }
}