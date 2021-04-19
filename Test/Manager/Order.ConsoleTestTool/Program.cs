using System;
using System.Threading;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Order;
using CMI.Utilities.Bus.Configuration;
using MassTransit;

namespace CMI.Manager.Order.ConsoleTestTool
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // Configure Bus
            var bus = BusConfigurator.ConfigureBus(MonitoredServices.NotMonitored, (cfg, host) => { });
            bus.Start();

            var archiveRecordId = 30409399;

            // Sending a testevent
            var ep = bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.DigitalisierungsAuftragErledigtEvent)).GetAwaiter().GetResult();
            ep.Send<IDigitalisierungsAuftragErledigt>(new
            {
                ArchiveRecordId = archiveRecordId,
                OrderItemId = 100,
                OrderDate = DateTime.Now,
                OrderUserId = "S51943653", // UserId Jörg
                OrderUserRolePublicClient = "Oe3"
            }).Wait();

            // Wait until the obove message has been consumed
            Thread.Sleep(10000);

            // Now send a sync command, to initiate the second cyncle of the test
            ep = bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.HarvestManagerSyncArchiveRecordMessageQueue)).GetAwaiter().GetResult();
            ep.Send<ISyncArchiveRecord>(new
            {
                MutationId = -1,
                ArchiveRecordId = archiveRecordId,
                Action = "Update"
            }).Wait();
        }
    }
}