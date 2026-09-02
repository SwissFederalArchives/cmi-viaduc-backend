using CMI.Web.Frontend.api.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace CMI.Web.Frontend.api.Elastic
{
    public class ElasticQuery
    {
        public ElasticQuery()
        {
            SearchParameters = new SearchParameters();
        }

        public Query Query { get; set; }

        public SearchParameters SearchParameters { get; set; }
    }
}