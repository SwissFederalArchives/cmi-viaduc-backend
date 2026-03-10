using CMI.Access.Common;
using CMI.Access.Sql.Viaduc.EF;
using CMI.Access.Sql.Viaduc.EF.Helper;
using CMI.Contract.Common;
using CMI.Contract.Parameter;
using CMI.Engine.Anonymization;
using CMI.Manager.Viaduc.Properties;
using Microsoft.Extensions.DependencyInjection;

namespace CMI.Manager.Viaduc.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static IServiceCollection Configure()
        {
            var services = new ServiceCollection();

            var connectionString = DbConnectionSetting.Default.ConnectionStringEF;
            services.AddScoped<ViaducDb>(_ => new ViaducDb(connectionString)); // ctor parameter

            services.AddScoped<ISearchIndexDataAccess, SearchIndexDataAccess>();
            services.AddScoped<IParameterHelper, ParameterHelper>();
            services.AddScoped<AccessHelper>(); // AsSelf()
            services.AddScoped<ICollectionAccess, CollectionAccess>();
            services.AddScoped<ICollectionManager, CollectionManager>();
            services.AddScoped<IManuelleKorrekturAccess, ManuelleKorrekturAccess>();
            services.AddScoped<IAnonymizationReferenceEngine, AnonymizationReferenceEngine>();
            services.AddScoped<IManuelleKorrekturManager, ManuelleKorrekturManager>();
            services.AddScoped<ISynchronisationManager, SynchronisationManager>();
            services.AddScoped<ISynchronisationAccess, SynchronisationAccess>();
            services.AddScoped<IViaducDataProvider, ViaducDataProvider>();

            return services;
        }
    }
}
