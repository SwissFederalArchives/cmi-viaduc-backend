using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace CMI.Access.Common;

public class ExternalKeysQueryProvider()
{
    public Query CreateExternalKeysQuery(string externalValue, string externalKey)
    {
        return new NestedQuery("externalKeys",
            new BoolQuery
            {
                Must =
                [
                    new MatchQuery(new Field("externalKeys.key"), FieldValue.String(externalKey)),
                    new QueryStringQuery(externalValue)
                    {
                        DefaultField = new Field("externalKeys.value"),
                        DefaultOperator = Operator.And,
                        AllowLeadingWildcard = false
                    },
                    new MatchQuery(new Field("externalKeys.value"), FieldValue.String(externalValue)),
                    new QueryStringQuery(externalKey)
                    {
                        DefaultField = new Field("externalKeys.key"),
                        DefaultOperator = Operator.And,
                        AllowLeadingWildcard = false
                    }
                ]
            });
    }
}