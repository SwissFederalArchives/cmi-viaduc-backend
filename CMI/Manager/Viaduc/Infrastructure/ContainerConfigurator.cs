using System;
using System.Reflection;
using Autofac;
using CMI.Access.Common;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Access.Sql.Viaduc.EF.Helper;
using CMI.Contract.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Monitoring;
using CMI.Contract.Parameter;
using CMI.Engine.Anonymization;
using CMI.Manager.Viaduc.Properties;
using CMI.Utilities.Bus.Configuration;
using MassTransit;

namespace CMI.Manager.Viaduc.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static ContainerBuilder Configure()
        {
            var builder = new ContainerBuilder();
            RegisterBus(builder);
            builder.RegisterType<SearchIndexDataAccess>().As<ISearchIndexDataAccess>();
            var connectionString = DbConnectionSetting.Default.ConnectionStringEF;
            builder.RegisterType<ViaducDb>().AsSelf().WithParameter(nameof(connectionString), connectionString);
            builder.RegisterType<ParameterHelper>().As<IParameterHelper>();

            builder.RegisterType<AccessHelper>().AsSelf();
            builder.RegisterType<CollectionAccess>().As<ICollectionAccess>();
            builder.RegisterType<CollectionManager>().As<ICollectionManager>();
            
            builder.RegisterType<ManuelleKorrekturAccess>().As<IManuelleKorrekturAccess>();
            builder.RegisterType<AnonymizationReferenceEngine>().As<IAnonymizationReferenceEngine>();
            builder.RegisterType<ManuelleKorrekturManager>().As<IManuelleKorrekturManager>();

            // SimpleConsumers Collection
            builder.RegisterType(typeof(SimpleConsumer<GetAllCollectionsRequest, GetAllCollectionsResponse, ICollectionManager>)).As(typeof(IConsumer<GetAllCollectionsRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetActiveCollectionsRequest, GetActiveCollectionsResponse, ICollectionManager>)).As(typeof(IConsumer<GetActiveCollectionsRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetCollectionsHeaderRequest, GetCollectionsHeaderResponse, ICollectionManager>)).As(typeof(IConsumer<GetCollectionsHeaderRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetCollectionRequest, GetCollectionResponse, ICollectionManager>)).As(typeof(IConsumer<GetCollectionRequest>));
            builder.RegisterType(typeof(SimpleConsumer<InsertOrUpdateCollectionRequest, InsertOrUpdateCollectionResponse, ICollectionManager>)).As(typeof(IConsumer<InsertOrUpdateCollectionRequest>));
            builder.RegisterType(typeof(SimpleConsumer<DeleteCollectionRequest, DeleteCollectionResponse, ICollectionManager>)).As(typeof(IConsumer<DeleteCollectionRequest>));
            builder.RegisterType(typeof(SimpleConsumer<BatchDeleteCollectionRequest, BatchDeleteCollectionResponse, ICollectionManager>)).As(typeof(IConsumer<BatchDeleteCollectionRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetPossibleParentsRequest, GetPossibleParentsResponse, ICollectionManager>)).As(typeof(IConsumer<GetPossibleParentsRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetImageRequest, GetImageResponse, ICollectionManager>)).As(typeof(IConsumer<GetImageRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetCollectionItemResultRequest, GetCollectionItemResultResponse, ICollectionManager>)).As(typeof(IConsumer<GetCollectionItemResultRequest>));

            // SimpleConsumers ManuelleKorrektur
            builder.RegisterType(typeof(SimpleConsumer<GetManuelleKorrekturRequest, GetManuelleKorrekturResponse, IManuelleKorrekturManager>)).As(typeof(IConsumer<GetManuelleKorrekturRequest>));
            builder.RegisterType(typeof(SimpleConsumer<InsertOrUpdateManuelleKorrekturRequest, InsertOrUpdateManuelleKorrekturResponse, IManuelleKorrekturManager>)).As(typeof(IConsumer<InsertOrUpdateManuelleKorrekturRequest>));
            builder.RegisterType(typeof(SimpleConsumer<DeleteManuelleKorrekturRequest, DeleteManuelleKorrekturResponse, IManuelleKorrekturManager>)).As(typeof(IConsumer<DeleteManuelleKorrekturRequest>));
            builder.RegisterType(typeof(SimpleConsumer<BatchDeleteManuelleKorrekturRequest, BatchDeleteManuelleKorrekturResponse, IManuelleKorrekturManager>)).As(typeof(IConsumer<BatchDeleteManuelleKorrekturRequest>));
            builder.RegisterType(typeof(SimpleConsumer<BatchAddManuelleKorrekturRequest, BatchAddManuelleKorrekturResponse, IManuelleKorrekturManager>)).As(typeof(IConsumer<BatchAddManuelleKorrekturRequest>));
            builder.RegisterType(typeof(SimpleConsumer<PublizierenManuelleKorrekturRequest, PublizierenManuelleKorrekturResponse, IManuelleKorrekturManager>)).As(typeof(IConsumer<PublizierenManuelleKorrekturRequest>));

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();

            return builder;
        }

        private static void RegisterBus(ContainerBuilder builder)
        {
            var helper = new ParameterBusHelper();
            BusConfigurator.ConfigureBus(builder, MonitoredServices.ViaducService,
                (cfg, ctx) => {
                    helper.SubscribeAllSettingsInAssembly(Assembly.GetExecutingAssembly(), cfg); });
        }
    }
}
