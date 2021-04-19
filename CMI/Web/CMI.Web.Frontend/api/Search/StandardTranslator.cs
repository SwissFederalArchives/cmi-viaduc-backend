using CMI.Access.Sql.Viaduc;
using CMI.Utilities.Common.Helpers;
using Nest;

namespace CMI.Web.Frontend.api.Search
{
    public class StandardTranslator : IFieldTranslator
    {
        public QueryContainer CreateQueryForField(SearchField field, UserAccess access)
        {
            return new QueryStringQuery
            {
                Query = field.Value.Escape(field.Key),
                DefaultField = field.Key,
                DefaultOperator = Operator.And,
                AllowLeadingWildcard = false
            };
        }
    }
}