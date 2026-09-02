using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Common.api;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Entities;
using CMI.Web.Frontend.api.Search;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CMI.Web.Frontend.api.Interfaces
{
    public interface IEntityProvider
    {
        Task<string[]> GetArchivplanRootNodes(UserAccess access, string role, string language);
        Task<string> GetArchivplanHtml(string id, UserAccess access, string role, string language);
        Task<string> GetArchivplanChildrenHtml(string id, UserAccess access, string role, string language);
        Task<Entity<T>> GetEntity<T>(string id, UserAccess access, Paging paging = null) where T : TreeRecord, new();
        Task<EntityResult<T>> GetEntities<T>(List<string> ids, UserAccess access, Paging paging = null) where T : TreeRecord, new();

        Task<List<Entity<T>>> GetResultAsEntities<T>(UserAccess access, ElasticQueryResult<T> result, EntityMetaOptions options = null)
            where T : TreeRecord, new();

        Task<ISearchResult> Search<T>(SearchParameters search, UserAccess access) where T : TreeRecord;
        Task<ISearchResult> SearchByReferenceCodeWithoutSecurity<T>(string signatur) where T : TreeRecord;
        string CheckSearchParameters(SearchParameters searchParameters, string language);
        Task<string[]> GetCountriesFromElastic();
    }
}