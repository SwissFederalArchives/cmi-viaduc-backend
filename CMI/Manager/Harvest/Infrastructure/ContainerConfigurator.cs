using CMI.Access.Harvest;
using CMI.Access.Harvest.ActaPro;
using CMI.Contract.Harvest;
using CMI.Contract.Parameter;
using CMI.Manager.Harvest.SyncLog;
using Microsoft.CSharp;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Caching;

namespace CMI.Manager.Harvest.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static IServiceCollection Configure()
        {
            var services = new ServiceCollection();

            services.AddSingleton<MemoryCache>(MemoryCache.Default);
            services.AddSingleton<CachedLookupData>();
            services.AddSingleton<TokenProvider>();
            services.AddSingleton<CSharpCodeProvider>();
            services.AddSingleton<ICachedHarvesterSetting, CachedHarvesterSetting>();

            services.AddScoped<AuthHandler>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAISDataProvider, ActaProAISDataProvider>();
            services.AddScoped<IArchiveRecordBuilder, ActaProArchiveRecordBuilder>();
            services.AddScoped<IDigitizationOrderBuilder, ActaProDigitizationOrderBuilder>();
            services.AddScoped<IDbSyncLogAccess, DbSyncLogAccess>();
            services.AddScoped<IHarvestManager, HarvestManager>();
            services.AddScoped<IParameterHelper, ParameterHelper>();
            services.AddScoped<IDbMetadataAccess, AISDataAccess>();
            services.AddScoped<IDbResyncAccess, AISDataAccess>();
            services.AddScoped<IDbExternalContentAccess, AISDataAccess>();
            services.AddScoped<IDbTestAccess, AISDataAccess>();

            services.AddHttpClient<IActaProClient, ActaProClient>()
                .AddHttpMessageHandler<AuthHandler>();

            return services;
        }
    }
}