using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CMI.Contract.Common;
using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json.Converters;
using Serilog;
using static System.String;

namespace CMI.Access.Common
{
    public class ElasticIndexHelper
    {
        private readonly string index;

        public ElasticIndexHelper(Uri elasticUri, string indexName = "archive")
        {
            var pool = new SingleNodeConnectionPool(elasticUri);
            var settings = new ConnectionSettings(pool,
                (serializer, values) => new JsonNetSerializer(
                    serializer, values, null, null,
                    new[] {new ExpandoObjectConverter()}));

            index = indexName;
            settings.DefaultIndex(indexName);
            settings.ThrowExceptions();
            Client = new ElasticClient(settings);
        }

        public IElasticClient Client { get; }

        public long CountDocuments
        {
            get
            {
                Client.Indices.Refresh(new RefreshRequest());
                return Client.Count<ElasticArchiveRecord>().Count;
            }
        }

        public void CreateIndex(string indexName)
        {
            string json;
            var assembly = GetType().Assembly;
            var resourceName = "CMI.Access.Common.ElasticRecordMapping.json";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException()))
            {
                json = reader.ReadToEnd();
            }

            var result = Client.LowLevel.DoRequest<StringResponse>(HttpMethod.PUT, index, json);

            if (!result.Success)
            {
                throw new InvalidOperationException(result.Body);
            }
        }

        public bool IndexExists(string indexName)
        {
            var indexes = Client.Cat.Indices();
            var aliases = Client.Cat.Aliases();
            return indexes.Records.Any(r => r.Index == indexName) || aliases.Records.Any(r => r.Alias == indexName);
        }

        public void DeleteIndex(string indexName)
        {
            var response = Client.Indices.Delete(indexName);

            if (!response.Acknowledged)
            {
                throw new Exception("Delete not acknowledged");
            }
        }

        public void Index(ElasticArchiveRecord record)
        {
            Client.Index(record, i => i.Id(record.ArchiveRecordId));
        }

        public void IndexBulk(IEnumerable<ElasticArchiveRecord> records)
        {
            var descriptor = new BulkDescriptor();

            foreach (var r in records)
            {
                descriptor.Index<ElasticArchiveRecord>(op => op.Document(r).Id(r.ArchiveRecordId));
            }

            Client.Bulk(descriptor);
        }

        public void Remove(string archiveRecordId)
        {
            // Let's check if the record we want to delete is available.
            var record = GetRecord(archiveRecordId, false);
            if (record != null)
            {
                Client.Delete<ElasticArchiveRecord>(archiveRecordId);
            }
        }

        public ElasticArchiveRecord GetRecord(string archiveRecordId, bool includeFulltextContent)
        {
            if (IsNullOrEmpty(archiveRecordId))
            {
                return null;
            }

            var r = Client.Search<ElasticArchiveRecord>(s =>
                s.Source(sf =>
                    {
                        if (!includeFulltextContent)
                        {
                            return sf.Excludes(e => e
                                .Fields("primaryData.items.content")
                            );
                        }

                        return sf;
                    })
                    .Query(q => q
                        .Ids(sel => sel.Values(archiveRecordId))
                    ));
            return r.Documents.FirstOrDefault();
        }

        public ElasticArchiveDbRecord GetDbRecord(string archiveRecordIdOrSignature, bool includeFulltextContent)
        {
            if (IsNullOrEmpty(archiveRecordIdOrSignature))
            {
                return null;
            }

            if (int.TryParse(archiveRecordIdOrSignature, out int  veId))
            {
                var result = Client.Search<ElasticArchiveDbRecord>(s =>
                    s.Source(sf =>
                        {
                            if (!includeFulltextContent)
                            {
                                return sf.Excludes(e => e
                                    .Fields("primaryData.items.content")
                                );
                            }
                            return sf;
                        })
                        .Query(q => q
                            .Ids(sel => sel.Values(veId))
                        ));

                //  Id is or must be unique
                return result.Documents.FirstOrDefault();
            }
            else
            {
               var searchRequest = new SearchRequest<ElasticArchiveDbRecord> { Query = CreateQueryForSignatur(archiveRecordIdOrSignature) };
               var result = Client.Search<ElasticArchiveDbRecord>(searchRequest);
               if (result.Documents.Count == 1)
               {
                   return result.Documents.FirstOrDefault();
               }
               
               if (result.Documents.Count > 1)
               {
                   throw new ArgumentOutOfRangeException("archiveRecordIdOrSignature", "The search for reference code has found more than one hit.");
               }
            }
       
            return null;
        }

        public void RemoveAll()
        {
            Client.DeleteByQuery<ElasticArchiveRecord>(q => q
                .Index(index)
                .Query(rq => rq
                    .MatchAll()));
        }

        public void UpdateTokens(string id, string[] primaryDataDownloadAccessTokens, string[] primaryDataFulltextAccessTokens,
            string[] metadataAccessTokens, string[] fieldAccessTokens)
        {
            var searchResponse = Client.Search<ElasticArchiveRecord>(s => s
                .Source(false)
                .Query(q => q
                    .Ids(c => c
                        .Values(id)
                    )
                )
            );

            if (searchResponse.Hits.Count == 0)
            {
                Log.Warning("Konnte die Tokens nicht aktualisieren für id {id}, weil Index Record nicht gefunden.", id);
                return;
            }

            if (searchResponse.Hits.Count > 1)
            {
                Log.Error("Konnte die Tokens nicht aktualisieren für id {id}, weil zu viele Hits ({count})gefunden wurden.", id,
                    searchResponse.Hits.Count);
                return;
            }

            // Für jede Token-Art muss mindestens ein Token (für BAR) geliefert werden.
            // Ansonsten stimmt etwas nicht und wir brechen den Update ab. Ausser für FieldAccessTokens, dann werden keine
            // individuellen FieldAccessToken hinterlegt. Nur die VE's gemäss Art. 12.3 anonymisiert werden bzw. geschützt sind, benötigen FieldAccessToken 
            if (primaryDataDownloadAccessTokens == null || primaryDataFulltextAccessTokens == null || metadataAccessTokens == null ||
                primaryDataDownloadAccessTokens.Length == 0 || primaryDataFulltextAccessTokens.Length == 0 || metadataAccessTokens.Length == 0)
            {
                Log.Warning(
                    "Für die id {id}, wurden keine Access-Tokens geliefert. Dies ist nicht erlaubt. Mindestens ein Access Token muss für jede Art vorhanden sein.\n" +
                    "Metadata-Access-Tokens: {metadataAccessTokens}\n" +
                    "PrimaryDataDownloadAccessTokens: {PrimaryDataDownloadAccessTokens}\n" +
                    "PrimaryDataFulltextAccessTokens: {PrimaryDataFulltextAccessTokens}",
                    id, metadataAccessTokens, primaryDataDownloadAccessTokens, primaryDataFulltextAccessTokens);
                return;
            }
            
            var updateResponse = Client.Update<ElasticArchiveRecord, object>
            (
                id,
                descriptor => descriptor.Doc(new
                {
                    PrimaryDataDownloadAccessTokens = primaryDataDownloadAccessTokens,
                    PrimaryDataFulltextAccessTokens = primaryDataFulltextAccessTokens,
                    MetadataAccessTokens = metadataAccessTokens,
                    FieldAccessTokens = fieldAccessTokens
                })
            );

            if (!updateResponse.IsValid)
            {
                Log.Error("Problem beim Update des Index für Id {id}. updateResponse={response}", id, updateResponse);
            }
        }

        public async Task<ElasticTestResponse> GetIndexHealth()
        {
            var isIndexReadOnly = await GetIndexIsReadonly();
            var catIndexResponse = await Client.Cat.IndicesAsync(s => s.Index(index));

            var firstPage = catIndexResponse?.Records?.FirstOrDefault();
            return new ElasticTestResponse
            {
                IsReadOnly = isIndexReadOnly,
                DocsCount = firstPage?.DocsCount,
                Health = firstPage?.Health?.ToLower(),
                Status = firstPage?.Status?.ToLower()
            };
        }

        private async Task<bool> GetIndexIsReadonly()
        {
            var indexSettingsResponse = await Client.Indices.GetSettingsAsync(index);

            var indexResponse =
                indexSettingsResponse.Indices?.FirstOrDefault(i => i.Key.Name.StartsWith(index, StringComparison.InvariantCultureIgnoreCase));
            
            if (indexResponse != null && indexResponse.Value.Value.Settings.ContainsKey(UpdatableIndexSettings.BlocksReadOnlyAllowDelete))
            {
                return bool.Parse(indexResponse.Value.Value.Settings[UpdatableIndexSettings.BlocksReadOnlyAllowDelete].ToString());
            }

            return false;
        }


        /// <summary>
        /// is copied from CMI.Web.Frontend.api.Search public static class ElasticQueryBuilder
        /// </summary>
        /// <param name="signatur"></param>
        /// <returns></returns>
        private static QueryContainer CreateQueryForSignatur(string signatur)
        {
            var boolQuery = new BoolQuery
            {
                Must = new QueryContainer[]
                {
                    new TermQuery
                    {
                        Field = "referenceCode",
                        Value = signatur
                    }
                }
            };
            return boolQuery;
        }
    }
}