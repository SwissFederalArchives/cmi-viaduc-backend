using CMI.Access.Sql.Viaduc;
using CMI.Utilities.Common.Helpers;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace CMI.Web.Frontend.api.Search
{
    public class StandardTranslator : IFieldTranslator
    {
        public Query CreateQueryForField(SearchField field, UserAccess access)
        {
            return new Query
            {
                QueryString = new QueryStringQuery(field.Value.Escape(field.Key))
                {
                    DefaultField = field.Key,
                    DefaultOperator = Operator.And,
                    AllowLeadingWildcard = false
                }
            };
        }
    }
}