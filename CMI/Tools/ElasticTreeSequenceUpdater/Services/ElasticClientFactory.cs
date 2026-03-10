using System;
using CMI.Tools.ElasticTreeSequenceUpdater.Properties;
using Nest;

namespace CMI.Tools.ElasticTreeSequenceUpdater.Services
{
    internal static class ElasticClientFactory
    {
        public static IElasticClient Create()
        {
            var url = Settings.Default.ElasticSearchUrl;
            var user = Settings.Default.ElasticSearchUsername;
            var pwd = Settings.Default.ElasticSearchPWD;

            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("ElasticSearchUrl not configured");

            var uri = new Uri(url);

            // ✅ Nest v7 ConnectionSettings
            var settings = new ConnectionSettings(uri)
                .DefaultIndex("archive") // default index
                .ThrowExceptions()       // throw on error
                .DisableDirectStreaming(); // easier debugging/logging

            if (!string.IsNullOrEmpty(user))
            {
                settings = settings.BasicAuthentication(user, pwd ?? "");
            }

            return new ElasticClient(settings);
        }
    }
}
