using System;
using System.Collections.Generic;
using System.Threading;
using Autofac;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Utilities.Bus.Configuration;
using MassTransit;

namespace CMI.Manager.Repository.ConsoleTestTool
{
    /// <summary>
    ///     Simple testing suite for integration tests
    /// </summary>
    internal class Program
    {
        public static ManualResetEvent DownloadReceivedEvent = new ManualResetEvent(false);
        public static ManualResetEvent AssetReadyReceivedEvent = new ManualResetEvent(false);

        private static void Main(string[] args)
        {
            // Configure Bus
            var bus = LoadBus();

            var packageId = string.Empty;
            while (packageId?.ToLowerInvariant() != "quit")
            {
                DownloadReceivedEvent = new ManualResetEvent(false);
                AssetReadyReceivedEvent = new ManualResetEvent(false);

                Console.WriteLine("Geben Sie eine AIP@DossierId Kennung ein, oder quit um das Programm zu beenden:");
                packageId = Console.ReadLine();
                Console.WriteLine("Geben D für DownlaodPackage oder A für AppendPackage an:");
                var messageType = Console.ReadLine();

                if (messageType == "D")
                {
                    DownloadPackage(bus, packageId);
                }
                else
                {
                    AppendPackage(bus, packageId);
                }
            }
        }

        private static IBusControl LoadBus()
        {
            // Configure Bus
            var containerBuilder = new ContainerBuilder(); 
            BusConfigurator.ConfigureBus(containerBuilder, MonitoredServices.NotMonitored, (cfg, ctx) =>
            {
                cfg.ReceiveEndpoint(BusConstants.RepositoryTestPackageDownloadedEventQueue, ec => { ec.Consumer(() => new MyConsumer()); });

                cfg.ReceiveEndpoint(BusConstants.AssetManagerAssetReadyEventQueue, ec => { ec.Consumer(() => new MyConsumer()); });
            });

            var container = containerBuilder.Build();
            var bus = container.Resolve<IBusControl>();
            bus.Start();

            return bus;
        }

        private static void AppendPackage(IBusControl bus, string packageId)
        {
            var endpoint = bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.RepositoryManagerArchiveRecordAppendPackageMessageQueue)).Result;
            Console.WriteLine("Sending message on queue...");
            var t = endpoint.Send<IArchiveRecordAppendPackage>(new
            {
                MutationId = 1,
                ArchiveRecord = new ArchiveRecord
                {
                    ArchiveRecordId = "9999",
                    Security = new ArchiveRecordSecurity
                    {
                        MetadataAccessToken = new List<string> {"BAR"}, PrimaryDataFulltextAccessToken = new List<string> {"BAR"},
                        PrimaryDataDownloadAccessToken = new List<string> {"BAR"}
                    },
                    Metadata = new ArchiveRecordMetadata {PrimaryDataLink = packageId}
                }
            });
        }

        private static async void DownloadPackage(IBusControl bus, string packageId)
        {
            var endpoint = await bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.RepositoryManagerDownloadPackageMessageQueue));
            var updateEndpoint = await bus.GetSendEndpoint(new Uri(bus.Address, BusConstants.IndexManagerUpdateArchiveRecordMessageQueue));
            Console.WriteLine("Sending message on queue...");
            // Need to update our dummy record, to not contain the primary data link. If this is not done, we get an exception
            // when creating the Gebrauchskopie, as we find two VE with the same primary data link.
            await updateEndpoint.Send<IUpdateArchiveRecord>(new
            {
                MutationId = 0,
                ArchiveRecord = new ArchiveRecord
                {
                    ArchiveRecordId = "9999",
                    Security = new ArchiveRecordSecurity
                    {
                        MetadataAccessToken = new List<string> {"BAR"}, PrimaryDataFulltextAccessToken = new List<string> {"BAR"},
                        PrimaryDataDownloadAccessToken = new List<string> {"BAR"}
                    },
                    Metadata = new ArchiveRecordMetadata {PrimaryDataLink = ""}
                }
            });
            await endpoint.Send<IDownloadPackage>(new
            {
                PackageId = packageId,
                ArchiveRecordId = 9999, // Id not relevant for our use case
                CallerId = "MyId",
                RetentionCategory = CacheRetentionCategory.UsageCopyPublic
            });


            Console.WriteLine("Waiting on queue to respond...");
            DownloadReceivedEvent.WaitOne();
            Console.WriteLine("Processed downloaded message");

            AssetReadyReceivedEvent.WaitOne();

            Console.WriteLine("Queue received message");
            Console.WriteLine("");
            Console.WriteLine("");
        }
    }
}