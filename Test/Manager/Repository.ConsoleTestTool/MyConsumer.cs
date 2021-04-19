using System;
using System.Threading.Tasks;
using CMI.Contract.Messaging;
using MassTransit;
using Newtonsoft.Json;

namespace CMI.Manager.Repository.ConsoleTestTool
{
    public class MyConsumer : IConsumer<IPackageDownloaded>, IConsumer<IAssetReady>
    {
        public Task Consume(ConsumeContext<IAssetReady> context)
        {
            Console.WriteLine("Received asset ready event...");
            var details = JsonConvert.SerializeObject(context.Message, Formatting.Indented);
            Console.WriteLine(details);
            Program.AssetReadyReceivedEvent.Set();
            return Task.CompletedTask;
        }

        public Task Consume(ConsumeContext<IPackageDownloaded> context)
        {
            Console.WriteLine("Received package downloaded event...");
            var details = JsonConvert.SerializeObject(context.Message.PackageInfo, Formatting.Indented);
            Console.WriteLine(details);
            Program.DownloadReceivedEvent.Set();
            return Task.CompletedTask;
        }
    }
}