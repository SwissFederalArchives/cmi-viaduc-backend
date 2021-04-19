using System.Collections.Generic;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.api;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Entities;
using CMI.Web.Frontend.api.Search;

namespace CMI.Web.Frontend.api.Interfaces
{
    public interface IEntityProvider
    {
        string GetArchivplanHtml(int id, UserAccess access, string role, string language);
        string GetArchivplanChildrenHtml(int id, UserAccess access, string role, string language);
        Entity<T> GetEntity<T>(int id, UserAccess access, Paging paging = null) where T : TreeRecord, new();
        EntityResult<T> GetEntities<T>(List<int> ids, UserAccess access, Paging paging = null) where T : TreeRecord, new();

        List<Entity<T>> GetResultAsEntities<T>(UserAccess access, ElasticQueryResult<T> result, EntityMetaOptions options = null)
            where T : TreeRecord, new();

        ISearchResult Search<T>(SearchParameters search, UserAccess access) where T : TreeRecord;
        ISearchResult SearchByReferenceCodeWithoutSecurity<T>(string signatur) where T : TreeRecord;
        string CheckSearchParameters(SearchParameters searchParameters, string language);
        string[] GetCountriesFromElastic();
    }
}