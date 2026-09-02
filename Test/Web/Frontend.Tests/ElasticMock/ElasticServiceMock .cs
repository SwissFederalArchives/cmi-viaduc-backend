using CMI.Access.Sql.Viaduc;
using CMI.Contract.Common;
using CMI.Web.Frontend.api.Elastic;
using CMI.Web.Frontend.api.Interfaces;
using CMI.Web.Frontend.api.Templates;
using Elastic.Clients.Elasticsearch.QueryDsl;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

namespace CMI.Web.Frontend.API.Tests.ElasticMock
{
    public class ElasticServiceMock : ElasticService
    {
        private readonly List<ElasticArchiveDbRecord> dataDb;
        public int CreateQueryForScopeIdAchieved { get; private set; }
        public ElasticServiceMock(IElasticClientProvider clientProvider, ISearchRequestBuilder searchRequestBuilder, IElasticSettings elasticSettings, List<TemplateField> internalFields, List<ElasticArchiveDbRecord> dataDb) :
            base(clientProvider, searchRequestBuilder, elasticSettings, internalFields )
        {
            this.dataDb = dataDb;
        }

        protected override BoolQuery QueryByIdOrExternalKey(string id)
        {
            CreateQueryForScopeIdAchieved++;
            return base.QueryByIdOrExternalKey(id);
        }

        protected override Task<ElasticArchiveDbRecord> GetElasticDbRecordById(string archiveRecordId, UserAccess access)
        {
            return  Task.FromResult(dataDb.Find(r => r.ArchiveRecordId == archiveRecordId));
        }
    }
}