using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Common.Properties;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using Elasticsearch.Net;
using Nest;
using Serilog;

namespace CMI.Access.Common
{
    public class SearchIndexDataAccess : ISearchIndexDataAccess, ITestSearchIndexDataAccess
    {
        private readonly ElasticIndexHelper helper;

        public SearchIndexDataAccess()
        {
            var uri = Settings.Default.ElasticSearchUrl;
            var node = new Uri(uri);
            helper = new ElasticIndexHelper(node);
        }

        public SearchIndexDataAccess(Uri address)
        {
            helper = new ElasticIndexHelper(address);
        }

        public void UpdateDocument(ElasticArchiveRecord elasticArchiveRecord)
        {
            helper.Index(elasticArchiveRecord);
        }

        public void RemoveDocument(string archiveRecordId)
        {
            helper.Remove(archiveRecordId);
        }

        public ElasticArchiveRecord FindDocument(string archiveRecordId, bool includeFulltextContent)
        {
            return helper.GetRecord(archiveRecordId, includeFulltextContent);
        }

        public ElasticArchiveRecord FindDocumentByPackageId(string packageId)
        {
            var query = new TermQuery
            {
                Field = (nameof(ElasticArchiveRecord.PrimaryDataLink) + ".keyword").ToLowerCamelCase(),
                Value = packageId
            };

            // Required to use typed search request, or else the default index setting is ignored.
            // See https://github.com/elastic/elasticsearch-net/issues/1906
            var searchRequest = new SearchRequest<ElasticArchiveRecord> {Query = query};
            var result = helper.Client.Search<ElasticArchiveRecord>(searchRequest);
            if (result.Total > 1)
            {
                throw new InvalidOperationException(
                    $"Searching for a packageId (AIP@DossierId) must return 1 or zero items. But {result.Total} items were found for packageId {packageId}");
            }

            if (result.Total == 0)
            {
                Log.Warning(
                    $"Did not find archive record when searching for packageId {packageId}. Query sent was {helper.Client.SourceSerializer.SerializeToString(searchRequest)}");
            }

            return result.Documents.FirstOrDefault();
        }

        public IEnumerable<ElasticArchiveRecord> GetChildren(string archiveRecordId, bool allLevels)
        {
            // Required to use typed search request, or else the default index setting is ignored.
            // See https://github.com/elastic/elasticsearch-net/issues/1906
            var searchRequest = new SearchRequest<ElasticArchiveRecord>();
            QueryContainer query;

            if (!allLevels)
            {
                query = new TermQuery
                {
                    Field = nameof(ElasticArchiveRecord.ParentArchiveRecordId).ToLowerCamelCase(),
                    Value = archiveRecordId
                };
            }
            else
            {
                var current = FindDocument(archiveRecordId, false);
                query = new BoolQuery
                {
                    Must = new List<QueryContainer>
                    {
                        new WildcardQuery
                            {Field = nameof(ElasticArchiveRecord.TreePath).ToLowerCamelCase(), Value = current != null ? current.TreePath + "*" : ""},
                        new NumericRangeQuery
                            {Field = nameof(ElasticArchiveRecord.TreeLevel).ToLowerCamelCase(), GreaterThan = current?.TreeLevel ?? 999}
                    }
                };
            }

            searchRequest.Query = query;

            var result = helper.Client.Search<ElasticArchiveRecord>(searchRequest);
            return result.Documents;
        }

        public void UpdateTokens(string id, string[] primaryDataDownloadAccessTokens, string[] primaryDataFulltextAccessTokens,
            string[] metadataAccessTokens)
        {
            helper.UpdateTokens(id, primaryDataDownloadAccessTokens, primaryDataFulltextAccessTokens, metadataAccessTokens);
        }

        public async Task<ElasticTestResponse> GetElasticIndexHealth()
        {
            return await helper.GetIndexHealth();
        }
    }
}