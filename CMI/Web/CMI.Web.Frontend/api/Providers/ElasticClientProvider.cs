using CMI.Contract.Common;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using System;
using System.Text;

namespace CMI.Web.Frontend.api.Providers
{
    public class ElasticClientProvider : IElasticClientProvider
    {
        public ElasticsearchClient GetElasticClient<T>(IElasticSettings settings, ElasticQueryResult<T> onResult = null) where T : TreeRecord
        {
            var clientSettings = new ElasticsearchClientSettings(new Uri(settings.BaseUrl));
            if (!string.IsNullOrEmpty(settings.Username) && !string.IsNullOrEmpty(settings.Password))
            {
                clientSettings = clientSettings.Authentication(new BasicAuthentication(settings.Username, settings.Password));
            }

            clientSettings = clientSettings.DefaultIndex(settings.DefaultIndex);

            if (onResult != null && settings.Debug != null && (settings.Debug.FetchRequestJson || settings.Debug.FetchResponseJson))
            {
                clientSettings
                    .DisableDirectStreaming()
                    .OnRequestCompleted(details =>
                    {
                        if (settings.Debug.FetchRequestJson && details.RequestBodyInBytes != null)
                        {
                            onResult.RequestRaw = Encoding.UTF8.GetString(details.RequestBodyInBytes);
                        }

                        if (settings.Debug.FetchResponseJson && details.ResponseBodyInBytes != null)
                        {
                            onResult.ResponseRaw = Encoding.UTF8.GetString(details.ResponseBodyInBytes);
                        }
                    });
            }

            clientSettings.ThrowExceptions();

            return new ElasticsearchClient(clientSettings);
        }
    }
}