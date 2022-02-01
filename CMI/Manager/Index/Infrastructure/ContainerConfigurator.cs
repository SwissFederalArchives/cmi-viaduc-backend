using System;
using System.IO;
using System.Reflection;
using Autofac;
using CMI.Access.Common;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
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

            // SimpleConsumers
            builder.RegisterType(typeof(SimpleConsumer<GetArchiveRecordsForPackageRequest, GetArchiveRecordsForPackageResponse, IIndexManager>)).As(typeof(IConsumer<GetArchiveRecordsForPackageRequest>));
            builder.RegisterType(typeof(SimpleConsumer<GetElasticLogRecordsRequest, GetElasticLogRecordsResponse, IElasticLogManager>)).As(typeof(IConsumer<GetElasticLogRecordsRequest>));

            // just register all the consumers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();

            return builder;
        }
    }
}