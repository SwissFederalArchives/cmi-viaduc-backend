using CMI.Access.Sql.Viaduc;
using CMI.Utilities.Common.Helpers;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;
using System.Collections.Generic;
using System.Linq;

namespace CMI.Web.Frontend.api.Search
{
    public class AllDataTranslator : IFieldTranslator
    {
        public Query CreateQueryForField(SearchField field, UserAccess access)
        {
            var values = access.CombinedTokens.Select(token => (FieldValue) token).ToList();

            var termQuery = new TermsQuery(new Field("primaryDataFulltextAccessTokens"), new TermsQueryField(values));

            return new BoolQuery
            {
                MinimumShouldMatch = 1,
                Should = new List<Query>
                {
                    new BoolQuery
                    {
                        Filter = new List<Query>{ termQuery },
                        Must = new List<Query>
                        {
                            new QueryStringQuery(@"all_\*:(" + field.Value.Escape() + ")")
                            {
                                DefaultOperator = Operator.And,
                                AllowLeadingWildcard = false
                            }
                        }
                    },
                    new QueryStringQuery(@"all_Metadata_\*:(" + field.Value.Escape() + ")")
                    {
                        DefaultOperator = Operator.And,
                        AllowLeadingWildcard = false
                    }
                }
            };
        }
    }
}