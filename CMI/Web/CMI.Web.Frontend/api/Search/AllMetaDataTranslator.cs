using CMI.Access.Sql.Viaduc;
using CMI.Utilities.Common.Helpers;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace CMI.Web.Frontend.api.Search
{
    public class AllMetaDataTranslator : IFieldTranslator
    {
        public Query CreateQueryForField(SearchField field, UserAccess access)
        {
            return new QueryStringQuery(@"all_Metadata_\*:(" + field.Value.Escape() + ")")
            {
                DefaultOperator = Operator.And,
                AllowLeadingWildcard = false
            };
        }
    }
}