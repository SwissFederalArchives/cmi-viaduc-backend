using CMI.Access.Sql.Viaduc;
using CMI.Utilities.Common.Helpers;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using System.Linq;

namespace CMI.Web.Frontend.api.Search
{
    public class AllPrimaryDataTranslator : IFieldTranslator
    {
        public Query CreateQueryForField(SearchField field, UserAccess access)
        {
            var values = access.CombinedTokens.Select(token => (FieldValue) token).ToList();
            return new BoolQuery
            {
                Filter = new Query[]
                {
                    new TermsQuery (new Field( "primaryDataFulltextAccessTokens"), new TermsQueryField(values))
                },
                Must = new Query[]
                {
                    new QueryStringQuery(@"all_Primarydata:(" + field.Value.Escape() + ")")
                    {
                        DefaultOperator = Operator.And,
                        AllowLeadingWildcard = false
                    }
                }
            };
        }
    }
}