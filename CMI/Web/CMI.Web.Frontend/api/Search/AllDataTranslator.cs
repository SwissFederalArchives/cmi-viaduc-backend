using CMI.Access.Sql.Viaduc;
using CMI.Utilities.Common.Helpers;
using Nest;

namespace CMI.Web.Frontend.api.Search
{
    public class AllDataTranslator : IFieldTranslator
    {
        public QueryContainer CreateQueryForField(SearchField field, UserAccess access)
        {
            return new BoolQuery
            {
                MinimumShouldMatch = 1,
                Should = new QueryContainer[]
                {
                    new BoolQuery
                    {
                        Filter = new QueryContainer[]
                        {
                            new TermsQuery
                            {
                                Field = "primaryDataFulltextAccessTokens",
                                Terms = access.CombinedTokens
                            }
                        },
                        Must = new QueryContainer[]
                        {
                            new QueryStringQuery
                            {
                                Query = @"all_\*:(" + field.Value.Escape() + ")",
                                DefaultOperator = Operator.And,
                                AllowLeadingWildcard = false
                            }
                        }
                    },
                    new QueryStringQuery
                    {
                        Query = @"all_Metadata_\*:(" + field.Value.Escape() + ")",
                        DefaultOperator = Operator.And,
                        AllowLeadingWildcard = false
                    }
                }
            };
        }
    }
}