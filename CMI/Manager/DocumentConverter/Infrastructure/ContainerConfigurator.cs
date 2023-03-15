using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Autofac;
using CMI.Manager.DocumentConverter.Abbyy;
using CMI.Manager.DocumentConverter.Properties;
using CMI.Manager.DocumentConverter.Extraction;
using CMI.Manager.DocumentConverter.Render;
using MassTransit;
using Serilog;

namespace CMI.Manager.DocumentConverter.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static ContainerBuilder Configure()
        {
            var builder = new ContainerBuilder();

            // register the different consumers and classes
            var enginePool = CreateEnginesPool();
            builder.RegisterInstance(enginePool).SingleInstance().ExternallyOwned();

            // register all extractors
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<TextExtractorBase>()
                .As<TextExtractorBase>();

            // register all renderers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<RenderProcessorBase>()
                .As<RenderProcessorBase>();

            builder.RegisterType<AbbyyWorker>().As<IAbbyyWorker>();
            builder.RegisterType<Extractor>().AsSelf();
            builder.RegisterType<Renderer>().AsSelf();
            builder.RegisterType<ConverterInstallationInfo>().AsSelf().SingleInstance();
            builder.Register(c => new AbbyyLicense(c.Resolve<IEnginesPool>())).SingleInstance();
            
            builder.RegisterType<DocumentManager>().As<IDocumentManager>();
            builder.RegisterType<SftpServer>().AsSelf().SingleInstance();

            // register all the consumers
            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();

            return builder;
        }

        private static IEnginesPool CreateEnginesPool()
        {
            try
            {
                var abbyyFrEngineDll= DocumentConverterSettings.Default.PathToAbbyyFrEngineDll;
                if (!string.IsNullOrEmpty(abbyyFrEngineDll) && File.Exists(abbyyFrEngineDll))
                {
                    var serial = DocumentConverterSettings.Default.AbbyySerialNumber;
                    var poolSize = DocumentConverterSettings.Default.AbbyyEnginePoolSize;
                    Log.Information($"Found Abbyy installation. Creating engine pool with {poolSize} engines.");
                    var pool = new EnginesPool(poolSize, serial, 90000)
                    {
                        AutoRecycleUsageCount = 100
                    };
                    Log.Information("Engine pool created.");
                    return pool;
                }

                Log.Warning("No Abbyy installation found. Using a empty engine pool.");
                return new DummyEnginePool();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to create Abbyy engine pool. Check if installation is present. Using an empty engine pool.");
                return new DummyEnginePool();
            }
        }
    }
}