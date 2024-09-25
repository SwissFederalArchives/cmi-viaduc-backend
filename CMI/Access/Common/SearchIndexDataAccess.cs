using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CMI.Access.Common.Properties;
using CMI.Contract.Common;
using CMI.Contract.Common.Extensions;
using CMI.Utilities.Common.Helpers;
using Elasticsearch.Net;
using Nest;
using Serilog;

namespace CMI.Access.Common
{
    public class SearchIndexDataAccess : ISearchIndexDataAccess, ITestSearchIndexDataAccess
    {
        private readonly ElasticIndexHelper helper;
        public const int  ElasticSearchHitLimit = 10000;

        public SearchIndexDataAccess()
        {
            var uri = Settings.Default.ElasticSearchUrl;
            string username = Settings.Default.ElasticSearchUsername;
            string pwd = Settings.Default.ElasticSearchPWD;
            var node = new Uri(uri);
            helper = new ElasticIndexHelper(node, username, pwd);
        }

        public SearchIndexDataAccess(Uri address, string username, string password)
        {
            helper = new ElasticIndexHelper(address, username, password);
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

        public ElasticArchiveRecord FindDocumentWithoutSecurity(string archiveRecordId, bool includeFulltextContent)
        {
            var record = FindDocument(archiveRecordId, includeFulltextContent);
            var dbRecord = FindDbDocument(archiveRecordId, includeFulltextContent);

            record.SetUnanonymizedValuesForAuthorizedUser(dbRecord);

            return record;

        }

        public ElasticArchiveDbRecord FindDbDocument(string archiveRecordIdOrSignature, bool includeFulltextContent)
        {
            return helper.GetDbRecord(archiveRecordIdOrSignature, includeFulltextContent);
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
            
            var record = result.Documents.FirstOrDefault();
            if (record != null && record.IsAnonymized)
            {
                var dbRecord = FindDbDocument(record.ArchiveRecordId, false);
                record.SetUnanonymizedValuesForAuthorizedUser(dbRecord);
            }
            return record;
        }

        public IEnumerable<ElasticArchiveRecord> GetChildren(string archiveRecordId, bool allLevels)
        {
            return GetChildrenInternal<ElasticArchiveRecord>(archiveRecordId, allLevels);
        }

        public IEnumerable<ElasticArchiveRecord> GetChildrenWithoutSecurity(string archiveRecordId, bool allLevels)
        {
            var children = GetChildrenInternal<ElasticArchiveRecord>(archiveRecordId, allLevels).ToList();
            var childrenDbRecord = GetChildrenInternal<ElasticArchiveDbRecord>(archiveRecordId, allLevels).ToList();

            // Now overwrite potentially anonymized fields with the clear values
            foreach (var child in children)
            {
                child.SetUnanonymizedValuesForAuthorizedUser(childrenDbRecord.First(c => c.ArchiveRecordId == child.ArchiveRecordId));
            }

            return children;
        }

        public void UpdateTokens(string id, string[] primaryDataDownloadAccessTokens, string[] primaryDataFulltextAccessTokens,
            string[] metadataAccessTokens, string[] fieldAccessTokens)
        {
            helper.UpdateTokens(id, primaryDataDownloadAccessTokens, primaryDataFulltextAccessTokens, metadataAccessTokens, fieldAccessTokens);
        }

        public async Task<ElasticTestResponse> GetElasticIndexHealth()
        {
            return await helper.GetIndexHealth();
        }


        private IEnumerable<T> GetChildrenInternal<T>(string archiveRecordId, bool allLevels) where T : class
        {
            // Required to use typed search request, or else the default index setting is ignored.
            // See https://github.com/elastic/elasticsearch-net/issues/1906
            var searchRequest = new SearchRequest<T>();
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
            searchRequest.From = 0;
            searchRequest.Size = ElasticSearchHitLimit;
            var sourceFilter = new SourceFilter()
            {
                Excludes = Infer.Fields(new[] {"primaryData.items.content", "thumbnail", "customFields.bildVorschau", "customFields.bildAnsicht"})
            };
            searchRequest.Source = sourceFilter;

            var result = helper.Client.Search<T>(searchRequest);
            return result.Documents;
        }
    }
}
