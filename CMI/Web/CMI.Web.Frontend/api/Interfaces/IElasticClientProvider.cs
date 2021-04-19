using CMI.Contract.Common;
using CMI.Web.Frontend.api.Elastic;
using Nest;

namespace CMI.Web.Frontend.api.Interfaces
{
    public interface IElasticClientProvider
    {
        IElasticClient GetElasticClient<T>(IElasticSettings settings, ElasticQueryResult<T> onResult = null) where T : TreeRecord;
    }
}