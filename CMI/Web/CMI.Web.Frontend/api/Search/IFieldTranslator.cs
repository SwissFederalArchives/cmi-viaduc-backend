using CMI.Access.Sql.Viaduc;
using Elastic.Clients.Elasticsearch.QueryDsl;

namespace CMI.Web.Frontend.api.Search
{
    public interface IFieldTranslator
    {
        Query CreateQueryForField(SearchField field, UserAccess access);
    }
}