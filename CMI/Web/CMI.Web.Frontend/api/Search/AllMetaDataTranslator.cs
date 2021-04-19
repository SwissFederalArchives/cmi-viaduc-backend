using CMI.Access.Sql.Viaduc;
using CMI.Utilities.Common.Helpers;
using Nest;

namespace CMI.Web.Frontend.api.Search
{
    public class AllMetaDataTranslator : IFieldTranslator
    {
        public QueryContainer CreateQueryForField(SearchField field, UserAccess access)
        {
            return new QueryStringQuery
            {
                Query = @"all_Metadata_\*:(" + field.Value.Escape() + ")",
                DefaultOperator = Operator.And,
                AllowLeadingWildcard = false
            };
        }
    }
}