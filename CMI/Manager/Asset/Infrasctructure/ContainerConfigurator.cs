using System.Reflection;
using Autofac;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Asset;
using CMI.Contract.Messaging;
using CMI.Contract.Parameter;
using CMI.Engine.Asset;
using CMI.Engine.Asset.ParameterSettings;
using CMI.Engine.Asset.PreProcess;
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

namespace CMI.Manager.Asset.Infrasctructure
{
    /// <summary>
    ///     MailHelper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static ContainerBuilder CreateContainerBuilder()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<CheckPendingDownloadRecordsJob>().AsSelf();
            builder.RegisterType<CheckPendingSyncRecordsJob>().AsSelf();
            builder.RegisterType<DeleteOldRecordsJob>().AsSelf();

            builder.RegisterType<AssetManager>().AsImplementedInterfaces();
            builder.RegisterType<TextEngine>().As<ITextEngine>().WithParameter("sftpLicenseKey", Settings.Default.SftpLicenseKey);
            builder.RegisterType<TextEngine>().As<ITextEngine>().WithParameter("sftpLicenseKey", Settings.Default.SftpLicenseKey);
            builder.RegisterType<Xsl2Processor>().AsSelf();
            builder.RegisterType<RenderEngine>().As<IRenderEngine>().WithParameter("sftpLicenseKey", Settings.Default.SftpLicenseKey);
            builder.RegisterType<CacheHelper>().As<ICacheHelper>().WithParameter("sftpLicenseKey", Settings.Default.SftpLicenseKey);

            builder.RegisterType<TransformEngine>().As<ITransformEngine>();
            builder.RegisterType<ParameterHelper>().As<IParameterHelper>();
            builder.RegisterType<MailHelper>().As<IMailHelper>();
            builder.RegisterType<DataBuilder>().As<IDataBuilder>();
            builder.RegisterType<ScanProcessor>().As<IScanProcessor>();
            builder.RegisterType<PdfManipulator>().As<IPdfManipulator>();
            builder.RegisterType<PreparationTimeCalculator>().As<IPreparationTimeCalculator>();
            builder.RegisterType<AssetPreparationEngine>().As<IAssetPreparationEngine>();
            builder.Register(GetAssetPreparationSettings).As<AssetPreparationSettings>();
            builder.RegisterType<PreProcessAnalyzerDetectAndFlagLargeDimensions>().AsSelf();
            builder.RegisterType<PreProcessAnalyzerOptimizePdf>().AsSelf();
            builder.RegisterType<FileResolution>().AsSelf();
            builder.Register(GetChannelAssignmentDefinition).As<ChannelAssignmentDefinition>();
            builder.Register(GetScansZusammenfassenSettings).As< ScansZusammenfassenSettings>();

            // register the different consumers and classes
            builder.RegisterType<PrimaerdatenAuftragAccess>().As<IPrimaerdatenAuftragAccess>().WithParameter("connectionString", DbConnectionSetting.Default.ConnectionString);
            builder.RegisterType<PasswordHelper>().AsSelf().SingleInstance().WithParameter("seed", Settings.Default.PasswordSeed).ExternallyOwned();
            builder.Register(GetPrefetchCount).As<RepositoryQueuesPrefetchCount>();
            builder.Register(GetAssetPackageSizeDefinition).As<AssetPackageSizeDefinition>();
            builder.Register(GetChannelAssignmentDefinition).As<ChannelAssignmentDefinition>();

            builder.RegisterType<PackagePriorizationEngine>().As<IPackagePriorizationEngine>();

            builder.Register(ctx =>
            {
                var helper = ctx.Resolve<IParameterHelper>();
                return helper.GetSetting<AssetPriorisierungSettings>();
            }).As<AssetPriorisierungSettings>();

            builder.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
                .AssignableTo<IConsumer>()
                .AsSelf();

            return builder;
        }

        private static AssetPreparationSettings GetAssetPreparationSettings(IComponentContext arg)
        {
            var parameterHelper = arg.Resolve<IParameterHelper>();
            return parameterHelper.GetSetting<AssetPreparationSettings>();
        }

        private static AssetPackageSizeDefinition GetAssetPackageSizeDefinition(IComponentContext arg)
        {
            // read and convert priorisierungs settings
            var parameterHelper = arg.Resolve<IParameterHelper>();
            var settings = parameterHelper.GetSetting<AssetPriorisierungSettings>();
            var packageSizeDefinition = JsonConvert.DeserializeObject<AssetPackageSizeDefinition>(settings.PackageSizes);
            return packageSizeDefinition;
        }

        private static ScansZusammenfassenSettings GetScansZusammenfassenSettings(IComponentContext arg)
        {
            // read and convert priorisierungs settings
            var parameterHelper = arg.Resolve<IParameterHelper>();
            return parameterHelper.GetSetting<ScansZusammenfassenSettings>();
        }


        private static ChannelAssignmentDefinition GetChannelAssignmentDefinition(IComponentContext arg)
        {
            // read and convert priorisierungs settings
            var parameterHelper = arg.Resolve<IParameterHelper>();
            var settings = parameterHelper.GetSetting<AssetPriorisierungSettings>();
            var chanelAssignements = JsonConvert.DeserializeObject<ChannelAssignmentDefinition>(settings.ChannelAssignments);
            return chanelAssignements;
        }

        private static RepositoryQueuesPrefetchCount GetPrefetchCount(IComponentContext arg)
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