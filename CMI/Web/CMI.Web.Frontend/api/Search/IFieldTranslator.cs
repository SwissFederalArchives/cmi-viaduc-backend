using CMI.Access.Sql.Viaduc;
using Nest;

namespace CMI.Web.Frontend.api.Search
{
    public interface IFieldTranslator
    {
        QueryContainer CreateQueryForField(SearchField field, UserAccess access);
    }
}