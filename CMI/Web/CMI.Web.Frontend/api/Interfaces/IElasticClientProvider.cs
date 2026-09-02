using CMI.Contract.Common;
using CMI.Web.Frontend.api.Elastic;
using Elastic.Clients.Elasticsearch;

namespace CMI.Web.Frontend.api.Interfaces
{
    public interface IElasticClientProvider
    {
        ElasticsearchClient GetElasticClient<T>(IElasticSettings settings, ElasticQueryResult<T> onResult = null) where T : TreeRecord;
    }
}