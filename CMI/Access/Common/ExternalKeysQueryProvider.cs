using Nest;

namespace CMI.Access.Common;

public class ExternalKeysQueryProvider 
{
    public QueryContainer CreateExternalKeysQuery(string externalValue,string externalKey)
    {
        return new NestedQuery
        {
            Path = "externalKeys",
            Query = new BoolQuery
            {
                Must = new QueryContainer[]
                {
                    new MatchQuery
                    {
                        Field = "externalKeys.key"
                    },
                    new QueryStringQuery
                    {
                        Query = externalValue,
                        DefaultField = "externalKeys.value",
                        DefaultOperator = Operator.And,
                        AllowLeadingWildcard = false
                    },
                    new MatchQuery
                    {
                        Field = "externalKeys.value"
                    },
                    new QueryStringQuery
                    {
                        Query = externalKey,
                        DefaultField = "externalKeys.key",
                        DefaultOperator = Operator.And,
                        AllowLeadingWildcard = false
                    }
                }
            }
        };
    }
}
