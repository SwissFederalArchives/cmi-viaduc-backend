using CMI.Access.Sql.Viaduc;
using CMI.Utilities.Common.Helpers;
using Nest;

namespace CMI.Web.Frontend.api.Search
{
    public class AllPrimaryDataTranslator : IFieldTranslator
    {
        public QueryContainer CreateQueryForField(SearchField field, UserAccess access)
        {
            return new BoolQuery
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
                        Query = @"all_Primarydata:(" + field.Value.Escape() + ")",
                        DefaultOperator = Operator.And,
                        AllowLeadingWildcard = false
                    }
                }
            };
        }
    }
}