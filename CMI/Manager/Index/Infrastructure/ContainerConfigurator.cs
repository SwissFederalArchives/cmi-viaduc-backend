using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using Autofac;
using CMI.Access.Common;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Access.Sql.Viaduc.EF.Helper;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.Anonymization;
using CMI.Engine.Anonymization.Properties;
using CMI.Manager.Index.Config;
using CMI.Utilities.Bus.Configuration;
using MassTransit;

namespace CMI.Manager.Index.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC Container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static ContainerBuilder Configure()
        {
            var builder = new ContainerBuilder();

            var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "customFieldsConfig.json");

            // register the different consumers and classes
            builder.RegisterType<IndexManager>().As<IIndexManager>();
            builder.RegisterType<SearchIndexDataAccess>().AsImplementedInterfaces();
            builder.RegisterType<LogDataAccess>().As<ILogDataAccess>();
            builder.RegisterType<ParameterHelper>().As<IParameterHelper>();

            builder.RegisterType<ElasticLogManager>().As<IElasticLogManager>();
            builder.RegisterType<CustomFieldsConfiguration>().AsSelf().SingleInstance().WithParameter("configurationFile", configFile);
            builder.Register(CreateHttpClient).As<HttpClient>().SingleInstance();
            builder.RegisterType<AnonymizationEngine>().As<IAnonymizationEngine>();
            var connectionString = Properties.Settings.Default.ConnectionStringEF;
            builder.RegisterType<ViaducDb>().AsSelf().WithParameter(nameof(connectionString), connectionString);

            builder.RegisterType<AccessHelper>().AsSelf();
            builder.RegisterType<ManuelleKorrekturAccess>().As<IManuelleKorrekturAccess>();
            builder.RegisterType<AnonymizationReferenceEngine>().As<IAnonymizationReferenceEngine>();
            // SimpleConsumers
            builder.RegisterType(typeof(SimpleConsumer<GetArchiveRecordsForPackageRequest, GetArchiveRecordsForPackageResponse, IIndexManager>)).As(typeof(IConsumer<GetArchiveRecordsForPackageRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetElasticLogRecordsRequest, GetElasticLogRecordsResponse, IElasticLogManager>)).As(typeof(IConsumer<GetElasticLogRecordsRequest>));

            // just register all the consumers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();

            return builder;
        }

        private static HttpClient CreateHttpClient(IComponentContext arg)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(Settings.Default.AnonymizationAddress);
            client.DefaultRequestHeaders.Add("X-ApiKey", Settings.Default.AnonymizationKey);
            return client;
        }
    }
}
