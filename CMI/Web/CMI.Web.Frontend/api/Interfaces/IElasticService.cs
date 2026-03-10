using System.Collections.Generic;
using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Utilities.ActaPro;
using CMI.Web.Common.api;
using CMI.Web.Frontend.api.Elastic;

namespace CMI.Web.Frontend.api.Interfaces
{
    public interface IElasticService
    {
        ElasticQueryResult<T> QueryForId<T>(string id, UserAccess access, bool translated = true) where T : TreeRecord;
        List<TreeRecord> QueryForParentId(string id, UserAccess access);
        ElasticQueryResult<T> QueryForIds<T>(IList<string> ids, UserAccess access, Paging p = null) where T : TreeRecord;
        ElasticQueryResult<T> QueryForIdsWithoutSecurityFilter<T>(IList<string> ids, Paging p = null) where T : TreeRecord;
        ElasticQueryResult<T> RunQuery<T>(ElasticQuery query, UserAccess access, bool translated = true) where T : TreeRecord;
        string[] GetLaender();
        ElasticQueryResult<T> RunQueryWithoutSecurityFilters<T>(ElasticQuery query) where T : TreeRecord;
        ElasticQueryResult<T> QueryForRootNodes<T>(UserAccess access) where T : TreeRecord;
        ActaProMappingProvider ActaProMappingProvider { get; }
         AccessTokens QueryTokensForId(string archiveRecordId);
    }
}