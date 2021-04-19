using System;
using System.IO;
using System.Linq;
using CMI.Manager.DocumentConverter.Abbyy;
using CMI.Manager.DocumentConverter.Properties;
using CMI.Manager.DocumentConverter.Extraction;
using CMI.Manager.DocumentConverter.Render;
using MassTransit;
using Ninject;
using Ninject.Activation;
using Ninject.Extensions.Conventions;
using Serilog;

namespace CMI.Manager.DocumentConverter.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static StandardKernel Configure()
        {
            var kernel = new StandardKernel();

            // register the different consumers and classes
            var enginePool = CreateEnginesPool();
            kernel.Bind<IEnginesPool>().ToConstant(enginePool).InSingletonScope();

            // register all extractors
            kernel.Bind(x =>
            {
                x.FromThisAssembly()
                    .SelectAllClasses()
                    .InheritedFrom<TextExtractorBase>()
                    .BindBase();
            });

            // register all renderers
            kernel.Bind(x =>
            {
                x.FromThisAssembly()
                    .SelectAllClasses()
                    .InheritedFrom<RenderProcessorBase>()
                    .BindBase();
            });

            kernel.Bind<IAbbyyWorker>().To<AbbyyWorker>();
            kernel.Bind<Extractor>().ToSelf();
            kernel.Bind<Renderer>().ToSelf();
            kernel.Bind<ConverterInstallationInfo>().ToSelf().InSingletonScope();
            kernel.Bind<IDocumentManager>().To<DocumentManager>();
            kernel.Bind<SftpServer>().ToSelf().InSingletonScope();

            // just register all the consumers using Ninject.Extensions.Conventions
            kernel.Bind(x =>
            {
                x.FromThisAssembly()
                    .SelectAllClasses()
                    .InheritedFrom<IConsumer>()
                    .BindToSelf();
            });

            return kernel;
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
                    var pool = new EnginesPool(poolSize, serial, 60000)
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