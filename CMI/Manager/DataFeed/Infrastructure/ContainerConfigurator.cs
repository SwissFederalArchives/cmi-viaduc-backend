using Autofac;
using MassTransit;
using System.Net.Http;
using System.Reflection;
using CMI.Manager.DataFeed.SyncLog;

namespace CMI.Manager.DataFeed.Infrastructure
{
    internal class ContainerConfigurator
    {
        public static ContainerBuilder Configure()
        {
            var builder = new ContainerBuilder();
            
            builder.RegisterType<DbSyncLogAccess>().As<IDbSyncLogAccess>();
            builder.RegisterType<CheckMutationQueueJob>().AsSelf();
            builder.RegisterType<RequeueMutationJob>().AsSelf();
            builder.RegisterType<DeleteOldSyncActionItemsJob>().AsSelf(); 
            builder.RegisterType<DataFeedManager>().As<IDataFeedManager>();

            builder.RegisterType<JobCancelToken>().As<ICancelToken>().SingleInstance().ExternallyOwned();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();

            return builder;
        }
    }
}