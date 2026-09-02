using CMI.Tools.ElasticTreeSequenceUpdater.Properties;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System;

namespace CMI.Tools.ElasticTreeSequenceUpdater.Services
{
    internal static class ElasticClientFactory
    {
        public static ElasticsearchClient Create()
        {
            var url = Settings.Default.ElasticSearchUrl;
            var user = Settings.Default.ElasticSearchUsername;
            var pwd = Settings.Default.ElasticSearchPWD;

            if (string.IsNullOrWhiteSpace(url))
                throw new InvalidOperationException("ElasticSearchUrl not configured");

            var uri = new Uri(url);

            var settings = new ElasticsearchClientSettings(uri);

            var indexName = "archive";

            if (!string.IsNullOrEmpty(user))
            {
                settings.Authentication(new BasicAuthentication(user, pwd));
            }
            settings.DefaultIndex(indexName);
            settings.ThrowExceptions();
            return new ElasticsearchClient(settings);
        }
    }
}
