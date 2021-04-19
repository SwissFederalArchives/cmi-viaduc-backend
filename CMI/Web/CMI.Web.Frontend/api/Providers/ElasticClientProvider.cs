using System;
using System.Text;
using CMI.Contract.Common;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json.Converters;

namespace CMI.Web.Frontend.api.Providers
{
    public class ElasticClientProvider : IElasticClientProvider
    {
        public IElasticClient GetElasticClient<T>(IElasticSettings settings, ElasticQueryResult<T> onResult = null) where T : TreeRecord
        {
            var connectionUri = new Uri(settings.BaseUrl);
            var connectionPool = new SingleNodeConnectionPool(connectionUri);

            var connectionSettings = new ConnectionSettings(connectionPool,
                (serializer, values) => new JsonNetSerializer(
                    serializer, values, null, null,
                    new[] {new ExpandoObjectConverter()}));

            connectionSettings.DefaultIndex(settings.DefaultIndex);
            if (!string.IsNullOrEmpty(settings.Username))
            {
                connectionSettings.BasicAuthentication(settings.Username, settings.Password);
            }

            if (onResult != null && settings.Debug != null && (settings.Debug.FetchRequestJson || settings.Debug.FetchResponseJson))
            {
                connectionSettings
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

            connectionSettings.ThrowExceptions();

            return new ElasticClient(connectionSettings);
        }
    }
}