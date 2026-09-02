using System.Collections.Generic;
using System.Threading.Tasks;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.ActaPro;
using CMI.Web.Common.api;
using CMI.Web.Frontend.api.Elastic;

namespace CMI.Web.Frontend.api.Interfaces
{
    public interface IElasticService
    {
         Task<ElasticQueryResult<T>> QueryForId<T>(string id, UserAccess access, bool translated = true) where T : TreeRecord;
         Task<List<TreeRecord>> QueryForParentId(UserAccess access, long scopeId, string actaProId);
         Task<List<TreeRecord>> QueryForParentId(string id, UserAccess access);
         Task<ElasticQueryResult<T>> QueryForIds<T>(IList<string> ids, UserAccess access, Paging p = null) where T : TreeRecord;
         Task<ElasticQueryResult<T>> QueryForIdsWithoutSecurityFilter<T>(IList<string> ids, Paging p = null) where T : TreeRecord;
         Task<ElasticQueryResult<T>> RunQuery<T>(ElasticQuery query, UserAccess access, bool translated = true) where T : TreeRecord;
         Task<string[]> GetLaender();
         Task<ElasticQueryResult<T>> RunQueryWithoutSecurityFilters<T>(ElasticQuery query) where T : TreeRecord;
         Task<ElasticQueryResult<T>> QueryForRootNodes<T>(UserAccess access) where T : TreeRecord;
         ActaProMappingProvider ActaProMappingProvider { get; }
         Task<AccessTokens> QueryTokensForId(string archiveRecordId);
    }
}
