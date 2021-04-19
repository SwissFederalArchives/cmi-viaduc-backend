using CMI.Web.Frontend.api.Search;
using Nest;

namespace CMI.Web.Frontend.api.Elastic
{
    public class ElasticQuery
    {
        public ElasticQuery()
        {
            SearchParameters = new SearchParameters();
        }

        public QueryContainer Query { get; set; }

        public SearchParameters SearchParameters { get; set; }
    }
}