using CMI.Access.Common.Properties;
using CMI.Contract.Common;
using CMI.Contract.Common.Extensions;
using CMI.Utilities.ActaPro;
using CMI.Utilities.Common.Helpers;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport.Extensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CMI.Access.Common
{
    public class SearchIndexDataAccess : ISearchIndexDataAccess, ITestSearchIndexDataAccess
    {
        private readonly ElasticIndexHelper helper;
        public const int ElasticSearchHitLimit = 10000;
        public ActaProMappingProvider ActaProMappingProvider { get; }

        public SearchIndexDataAccess()
        {
            var uri = Settings.Default.ElasticSearchUrl;
            var username = Settings.Default.ElasticSearchUsername;
            var pwd = Settings.Default.ElasticSearchPWD;
            var node = new Uri(uri);
            helper = new ElasticIndexHelper(node, username, pwd);
            ActaProMappingProvider = new ActaProMappingProvider();
        }

        public SearchIndexDataAccess(Uri address, string username, string password)
        {
            helper = new ElasticIndexHelper(address, username, password);
        }

        public async Task UpdateDocument(ElasticArchiveRecord elasticArchiveRecord)
        {
            await helper.Index(elasticArchiveRecord);
        }

        public async Task RemoveDocument(string archiveRecordId)
        {
            await helper.Remove(archiveRecordId);
        }

        public async Task<ElasticArchiveRecord> FindDocument(string archiveRecordId, MetadataToExclude metadataToExclude)
        {
            return await helper.GetRecord(archiveRecordId, metadataToExclude);
        }

        public async Task<ElasticArchiveRecord> FindDocumentWithoutSecurity(string archiveRecordId, MetadataToExclude metadataToExclude)
        {
            var record = await FindDocument(archiveRecordId, metadataToExclude);
            var dbRecord = await FindDbDocument(archiveRecordId, metadataToExclude);

            record.SetUnanonymizedValuesForAuthorizedUser(dbRecord);

            return record;

        }

        public async Task<ElasticArchiveDbRecord> FindDbDocument(string archiveRecordIdOrSignature, MetadataToExclude metadataToExclude)
        {
            return await helper.GetDbRecord(archiveRecordIdOrSignature, metadataToExclude);
        }

        public async Task<ElasticArchiveRecord> FindDocumentByPackageId(string packageId)
        {
            var query = new TermsQuery(new Field((nameof(ElasticArchiveRecord.PrimaryDataLink) + ".keyword").ToLowerCamelCase()),
                new TermsQueryField(new List<FieldValue> { packageId }));

            // Required to use typed search request, or else the default index setting is ignored.
            // See https://github.com/elastic/elasticsearch-net/issues/1906
            var searchRequest = new SearchRequest<ElasticArchiveRecord> { Query = query };
            var result = await helper.Client.SearchAsync<ElasticArchiveRecord>(searchRequest);
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
                var dbRecord = await FindDbDocument(record.ArchiveRecordId, MetadataToExclude.OCRContentAndFiles);
                record.SetUnanonymizedValuesForAuthorizedUser(dbRecord);
            }

            return record;
        }

        public async Task<List<ElasticArchiveRecord>> FindDocument(Dictionary<string, string> queryTerms, int pageSize)
        {

            var query = new Query
            {
                Bool = new BoolQuery
                {
                    Should = queryTerms
                        .Select(qt => new Query
                        {
                            Term = new TermQuery(new Field(qt.Key), FieldValue.String(qt.Value))
                        })
                        .ToList()
                }
            };

            var searchRequest = new SearchRequest<ElasticArchiveRecord> { Query = query };
            if (pageSize > 0)
            {
                searchRequest.Size = pageSize;
            }

            var result = await helper.Client.SearchAsync<ElasticArchiveRecord>(searchRequest);

            var list = new List<ElasticArchiveRecord>(result.Documents);

            return list;

        }

        public async Task<IEnumerable<ElasticArchiveRecord>> GetChildren(string archiveRecordId, string scopeId, bool allLevels)
        {
            return await GetChildrenInternal<ElasticArchiveRecord>(archiveRecordId, scopeId, allLevels);
        }


        public async Task<IEnumerable<ElasticArchiveRecord>> GetChildrenWithoutSecurity(string archiveRecordId, string scopeId, bool allLevels)
        {
            var children = (await GetChildrenInternal<ElasticArchiveRecord>(archiveRecordId, scopeId, allLevels)).ToList();
            var childrenDbRecord = (await GetChildrenInternal<ElasticArchiveDbRecord>(archiveRecordId, scopeId, allLevels)).ToList();

            // Now overwrite potentially anonymized fields with the clear values
            foreach (var child in children)
            {
                child.SetUnanonymizedValuesForAuthorizedUser(childrenDbRecord.First(c => c.ArchiveRecordId == child.ArchiveRecordId));
            }

            return children;
        }

        public async Task UpdateTokens(string id, string[] primaryDataDownloadAccessTokens, string[] primaryDataFulltextAccessTokens,
            string[] metadataAccessTokens, string[] fieldAccessTokens)
        {
            await helper.UpdateTokens(id, primaryDataDownloadAccessTokens, primaryDataFulltextAccessTokens, metadataAccessTokens, fieldAccessTokens);
        }


        public async Task<ElasticTestResponse> GetElasticIndexHealth()
        {
            return await helper.GetIndexHealth();
        }


        private async Task<IEnumerable<T>> GetChildrenInternal<T>(string archiveRecordId, string scopeId, bool allLevels) where T : class
        {
            // Required to use typed search request, or else the default index setting is ignored.
            // See https://github.com/elastic/elasticsearch-net/issues/1906
            var searchRequest = new SearchRequest<T>();
            Query query;

            if (!allLevels && int.TryParse(scopeId, out _))
            {
                // We search for both the docKey and the scopeId
                query = new BoolQuery
                {
                    Should = new List<Query>
                    {
                        new TermsQuery(
                            new Field(nameof(ElasticArchiveRecord.ParentArchiveRecordId).ToLowerCamelCase()),
                            new TermsQueryField(new List<FieldValue> {scopeId})
                        ),
                        new TermsQuery(
                            new Field(nameof(ElasticArchiveRecord.ParentArchiveRecordId).ToLowerCamelCase()),
                            new TermsQueryField(new List<FieldValue> {archiveRecordId})
                        )
                    }
                };
            }
            else if (!allLevels)
            {
                // As there was numeric scopeId provided, we only search for the docKey/archiveRecordId.
                Log.Debug("GetChildrenInternal with docKey: {archiveRecordId}", archiveRecordId);
                query = new TermsQuery(
                    new Field(nameof(ElasticArchiveRecord.ParentArchiveRecordId).ToLowerCamelCase()),
                    new TermsQueryField(new List<FieldValue> { archiveRecordId })
                );
            }
            else
            {
                Log.Debug("GetChildrenInternal with all levels: {archiveRecordId} and externalKey {scopeId}", archiveRecordId, scopeId);

                // This is the case if a record was not yet synced. It contains the treePath with the concatenated scope Ids
                // To make sure we get all child records, we must also search the tree path with the mapped ids.
                var current = await FindDocument(scopeId, MetadataToExclude.OCRContentAndFiles);
                // It is also possible that an ActaPro record is associated with the ScopeId.
                if (int.TryParse(current?.ArchiveRecordId, out _))
                {
                    var scopeIds = new List<int>();
                    for (var i = 0; i < current.TreePath.Length; i += 10)
                    {
                        scopeIds.Add(int.Parse(current.TreePath.Substring(i, 10)));
                    }

                    var uuIds = scopeIds.Select(s => ActaProMappingProvider.GetActaProId(s.ToString()) ?? s.ToString());
                    var actaProTreePath = string.Join("", uuIds);

                    query = new BoolQuery
                    {
                        Must = new List<Query>
                        {
                            new NumberRangeQuery(nameof(ElasticArchiveRecord.TreeLevel).ToLowerCamelCase())
                            {
                                Gt = current?.TreeLevel ?? 999
                            },
                            new BoolQuery
                            {
                                Should = new List<Query>
                                {
                                    new WildcardQuery(nameof(ElasticArchiveRecord.TreePath).ToLowerCamelCase())
                                    {
                                        Value = current.TreePath + "*"
                                    },
                                    new WildcardQuery(nameof(ElasticArchiveRecord.TreePath).ToLowerCamelCase())
                                    {
                                        Value = !string.IsNullOrEmpty(actaProTreePath) ? actaProTreePath + "*" : ""
                                    }
                                }
                            }
                        }
                    };
                }
                else
                {
                    query = new BoolQuery
                    {
                        Must = new List<Query>
                        {
                            new NumberRangeQuery(nameof(ElasticArchiveRecord.TreeLevel).ToLowerCamelCase())
                            {
                                Gt = current?.TreeLevel ?? 999
                            },
                            new WildcardQuery(nameof(ElasticArchiveRecord.TreePath).ToLowerCamelCase())
                            {
                                Value = current != null ? current.TreePath + "*" : ""
                            },
                        }
                    };
                }
            }
            
            // Execute the query
            searchRequest.Query = query;
            searchRequest.From = 0;
            searchRequest.Size = ElasticSearchHitLimit;
            var sourceFilter = new SourceFilter
            {
                Excludes = Infer.Fields("primaryData.items", "thumbnail", "customFields.bildVorschau", "customFields.bildAnsicht")
            };
            searchRequest.Source = new SourceConfig(sourceFilter);

            var result = await helper.Client.SearchAsync<T>(searchRequest);
            return result.Documents;
        }
    }
}
