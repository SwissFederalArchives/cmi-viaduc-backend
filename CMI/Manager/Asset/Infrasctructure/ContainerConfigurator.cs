using CMI.Access.Sql.Viaduc;
using CMI.Contract.Asset;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.Asset;
using CMI.Engine.MailTemplate;
using CMI.Engine.Security;
using CMI.Manager.Asset.Jobs;
using CMI.Manager.Asset.ParameterSettings;
using CMI.Manager.Asset.Properties;
using CMI.Utilities.Bus.Configuration;
using CMI.Utilities.Cache.Access;
using CMI.Utilities.Template;
using MassTransit;
using Newtonsoft.Json;
using Ninject;
using Ninject.Activation;
using Ninject.Extensions.Conventions;

namespace CMI.Manager.Asset.Infrasctructure
{
    /// <summary>
    ///     MailHelper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static StandardKernel Configure()
        {
            var kernel = new StandardKernel();

            // register the different consumers and classes
            kernel.Bind<IAssetManager>().To(typeof(AssetManager));
            kernel.Bind<IOcrTester>().To(typeof(AssetManager));
            kernel.Bind<ITextEngine>().To(typeof(TextEngine)).WithConstructorArgument("sftpLicenseKey", Settings.Default.SftpLicenseKey);
            kernel.Bind<IRenderEngine>().To(typeof(RenderEngine)).WithConstructorArgument("sftpLicenseKey", Settings.Default.SftpLicenseKey);
            kernel.Bind<ITransformEngine>().To(typeof(TransformEngine));
            kernel.Bind<ICacheHelper>().To(typeof(CacheHelper)).WithConstructorArgument("sftpLicenseKey", Settings.Default.SftpLicenseKey); ;
            kernel.Bind<IParameterHelper>().To(typeof(ParameterHelper));
            kernel.Bind<IMailHelper>().To(typeof(MailHelper));
            kernel.Bind<IDataBuilder>().To(typeof(DataBuilder));
            kernel.Bind<IScanProcessor>().To(typeof(ScanProcessor));
            kernel.Bind<IPreparationTimeCalculator>().To(typeof(PreparationTimeCalculator));
            kernel.Bind<IPrimaerdatenAuftragAccess>().To<PrimaerdatenAuftragAccess>()
                .WithConstructorArgument(arg => DbConnectionSetting.Default.ConnectionString);
            kernel.Bind<PasswordHelper>().ToSelf().InSingletonScope().WithConstructorArgument("seed", Settings.Default.PasswordSeed);
            kernel.Bind<RepositoryQueuesPrefetchCount>().ToMethod(GetPrefetchCount);
            kernel.Bind<AssetPackageSizeDefinition>().ToMethod(GetAssetPackageSizeDefinition);
            kernel.Bind<ChannelAssignmentDefinition>().ToMethod(GetChannelAssignmentDefinition);
            kernel.Bind<IPackagePriorizationEngine>().To<PackagePriorizationEngine>();
            kernel.Bind<CheckPendingDownloadRecordsJob>().ToSelf();
            kernel.Bind<CheckPendingSyncRecordsJob>().ToSelf();
            kernel.Bind<AssetPriorisierungSettings>().ToMethod(ctx =>
            {
                var helper = ctx.Kernel.Get<IParameterHelper>();
                return helper.GetSetting<AssetPriorisierungSettings>();
            });

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

        private static AssetPackageSizeDefinition GetAssetPackageSizeDefinition(IContext arg)
        {
            // read and convert priorisierungs settings
            var parameterHelper = arg.Kernel.Get<IParameterHelper>();
            var settings = parameterHelper.GetSetting<AssetPriorisierungSettings>();
            var packageSizeDefinition = JsonConvert.DeserializeObject<AssetPackageSizeDefinition>(settings.PackageSizes);
            return packageSizeDefinition;
        }

        private static ChannelAssignmentDefinition GetChannelAssignmentDefinition(IContext arg)
        {
            // read and convert priorisierungs settings
            var parameterHelper = arg.Kernel.Get<IParameterHelper>();
            var settings = parameterHelper.GetSetting<AssetPriorisierungSettings>();
            var chanelAssignements = JsonConvert.DeserializeObject<ChannelAssignmentDefinition>(settings.ChannelAssignments);
            return chanelAssignements;
        }

        private static RepositoryQueuesPrefetchCount GetPrefetchCount(IContext arg)
        {
            var retVal = new RepositoryQueuesPrefetchCount
            {
                DownloadQueuePrefetchCount =
                    BusConfigurator.GetPrefetchCountForEndpoint(BusConstants.RepositoryManagerDownloadPackageMessageQueue) ?? 4,
                SyncQueuePrefetchCount =
                    BusConfigurator.GetPrefetchCountForEndpoint(BusConstants.RepositoryManagerArchiveRecordAppendPackageMessageQueue) ?? 4
            };

            return retVal;
        }
    }
}