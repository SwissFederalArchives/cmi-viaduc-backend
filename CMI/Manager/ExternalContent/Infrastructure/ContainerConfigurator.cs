using CMI.Access.Harvest;
using CMI.Access.Harvest.ActaPro;
using CMI.Contract.Harvest;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CMI.Manager.ExternalContent.Infrastructure
{
    /// <summary>
    ///     Helper class for configuring the IoC container.
    /// </summary>
    internal class ContainerConfigurator
    {
        public static IServiceCollection Configure()
        {
            var services = new ServiceCollection();

            services.AddScoped<IExternalContentManager, ExternalContentManager>();
            services.AddScoped<IDbExternalContentAccess, AISDataAccess>();
            services.AddScoped<IAISDataProvider, ActaProAISDataProvider>();
            services.AddScoped<IArchiveRecordBuilder, ActaProArchiveRecordBuilder>();
            services.AddScoped<IDigitizationOrderBuilder, ActaProDigitizationOrderBuilder>();
            services.AddScoped<IActaProClient, ActaProClient>();

            services.AddSingleton<CachedLookupData>();
            services.AddSingleton<TokenProvider>();
            services.AddScoped<AuthHandler>();
            services.AddScoped<IAuthService, AuthService>();

            services.AddHttpClient<IActaProClient, ActaProClient>()
                .AddHttpMessageHandler<AuthHandler>();

            return services;
        }
    }
}