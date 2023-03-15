
using System;
using System.Reflection;
using Autofac;
using CMI.Access.Repository;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Contract.Repository;
using CMI.Engine.PackageMetadata;
using CMI.Manager.Index.Consumer;
using CMI.Manager.Repository;
using CMI.Utilities.Bus.Configuration;
using MassTransit;
using Serilog;

namespace CMI.Utilities.DigitalRepository.PrimaryDataHarvester
{
    internal class ContainerConfigurator
    {
        public static ContainerBuilder Configure()
        {
            var builder = new ContainerBuilder();


            BusConfigurator.ConfigureBus(builder, MonitoredServices.NotMonitored, (cfg, ctx) =>
            {
                cfg.UseNewtonsoftJsonSerializer();
            });

            builder.RegisterType<RepositoryManager>().As<IRepositoryManager>();
            builder.RegisterType<RepositoryConnectionFactory>().As<IRepositoryConnectionFactory>();
            builder.RegisterType<RepositoryDataAccess>().As<IRepositoryDataAccess>();
            builder.RegisterType<PackageValidator>().As<IPackageValidator>();
            builder.RegisterType<MetadataDataAccess>().As<IMetadataDataAccess>();
            builder.RegisterType<PackageHandler>().As<IPackageHandler>();
            builder.RegisterType<ParameterHelper>().As<IParameterHelper>();

            // register all the consumers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();


            builder.Register(CreateFindArchiveRecordRequestClient);
            builder.Register(GetArchiveRecordsForPackageRequestClientCallback);
            builder.RegisterType<PrimaryDataHarvester>().AsSelf();

            return builder;
        }

        private static IRequestClient<FindArchiveRecordRequest> CreateFindArchiveRecordRequestClient(IComponentContext context)
        {
            var requestTimeout = TimeSpan.FromMinutes(10);
            var bus = context.Resolve<IBusControl>();
            return bus.CreateRequestClient<FindArchiveRecordRequest>(new Uri(new Uri(BusConfigurator.Uri), BusConstants.IndexManagerFindArchiveRecordMessageQueue), requestTimeout);
        }

        private static IRequestClient<GetArchiveRecordsForPackageRequest>
            GetArchiveRecordsForPackageRequestClientCallback(IComponentContext context)
        {
            var serviceUrl = string.Format(BusConstants.IndexManagagerRequestBase, nameof(GetArchiveRecordsForPackageRequest));
            var requestTimeout = TimeSpan.FromMinutes(10);
            var bus = context.Resolve<IBusControl>();

            return bus.CreateRequestClient<GetArchiveRecordsForPackageRequest>(new Uri(bus.Address, serviceUrl), requestTimeout);
        }
    }
}
