using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CMI.Contract.Common;
using CMI.Utilities.ActaPro;
using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using SourceFilter = Nest.SourceFilter;

namespace CMI.Access.Common
{
    public class ElasticIndexHelper
    {
        private readonly string index;

        public ElasticIndexHelper(Uri elasticUri, string userName = "", string pwd = "", string indexName = "archive")
        {
            var pool = new SingleNodeConnectionPool(elasticUri);
            var settings = new ConnectionSettings(pool,
                (serializer, values) => new JsonNetSerializer(
                    serializer, values, null, null,
                    new[] { new ExpandoObjectConverter() }));

            index = indexName;

            if (!string.IsNullOrEmpty(userName))
            {
                settings.BasicAuthentication(userName, pwd);
            }
            settings.DefaultIndex(indexName);
            settings.ThrowExceptions();
            // settings.DisableDirectStreaming(true);  Zum Debuggen aktivieren, damit man die Requests und Responses sieht.      
            Client = new ElasticClient(settings);
        }


        public ElasticIndexHelper(IElasticClient client)
        {
            Client = client;
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
            Log.Debug("Writing the record into the elastic index. ArchivRecordId: {archiveRecordId}. Callstack is: {callstack} Data is: {record}", record.ArchiveRecordId, Environment.StackTrace, JsonConvert.SerializeObject(record));

            var response = Client.Index(record, i => i.Id(record.ArchiveRecordId));
            if (!response.IsValid)
            {
                Log.Error("Problem beim Indexieren des Records für archivRecordId: {archiveRecordId}. Response: {response}", record.ArchiveRecordId, JsonConvert.SerializeObject(response));
                throw new InvalidOperationException($"Problem beim Indexieren des Records für archivRecordId: {record.ArchiveRecordId}. Response: {response}");
            }

            // Damit der Index aktualisiert ist, bevor die Methode zurückkehrt.
            Task.Delay(500).ConfigureAwait(false).GetAwaiter().GetResult();

            Log.Information("Successfully updated the data in elastic index for archivRecordId: {archiveRecordId}. Response valid: {valid}, {result}, DebugInfo: {debug} Data is: {record}", 
                record.ArchiveRecordId, 
                response.IsValid, response.Result.ToString(), response.DebugInformation, JsonConvert.SerializeObject(record));
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
            var record = GetRecord(archiveRecordId, MetadataToExclude.OCRContentAndFiles);
            if (record != null)
            {
                Client.Delete<ElasticArchiveRecord>(record.ArchiveRecordId);
            }
        }

        /// <summary>
        /// Findet einen Record anhand der Id.
        /// Es kann die alte scopeId oder der neue DocKey übergeben werden.
        /// </summary>
        /// <param name="archiveRecordId"></param>
        /// <param name="metadataToExclude"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public ElasticArchiveRecord GetRecord(string archiveRecordId, MetadataToExclude metadataToExclude)
        {
            if (string.IsNullOrEmpty(archiveRecordId))
            {
                return null;
            }
            
            // Im Falle einer scopeArchivId
            if (int.TryParse(archiveRecordId, out int veId))
            {
                // Es wird versucht die scopeId über eine Suche nach dem ExternalKey zu finden.
                // Nachdem ein Record neu synchronisiert ist, ist die scopeId nur noch im ExternalKey zu finden.
                Log.Debug("Trying to fetch ElasticRecord by doing a search with the scopeId using the externalKey {veId}", veId);
                var scopeIdTranslator = new ExternalKeysQueryProvider();
                var searchRequest = new SearchRequest<ElasticArchiveRecord>
                {
                    Query = scopeIdTranslator.CreateExternalKeysQuery(archiveRecordId, "scopeArchiv"),
                    Source = new SourceFilter()
                    {
                        Excludes = GetExcludeFields(metadataToExclude)
                    }
                };
                var result = Client.Search<ElasticArchiveRecord>(searchRequest);

                // Back Up.
                // Ist die Suche anhand des ExternalKey erfolglos, versuchen wir die Suche nach dem PrimaryKey.
                // Das ist der Fall, wenn der Record noch gar nie neu synchronisiert wurde.
                if (result.Documents.Count == 0)
                {
                    Log.Debug("Trying to fetch ElasticRecord by doing a search with the scopeId using the elastic _id {veId}", veId);
                    result = Client.Search<ElasticArchiveRecord>(s =>
                        s.Source(sf =>
                            {
                                return metadataToExclude switch
                                {
                                    MetadataToExclude.OCRContentAndFiles => sf.Excludes(e => e.Fields("primaryData.items")),
                                    MetadataToExclude.OCRContent => sf.Excludes(e => e.Fields("primaryData.items.content")),
                                    MetadataToExclude.Nothing => sf,
                                    _ => throw new ArgumentOutOfRangeException(nameof(metadataToExclude), metadataToExclude, null)
                                };
                            })
                            .Query(q => q
                                .Ids(sel => sel.Values(veId))
                            ));
                }

                if (result.Documents.Count > 1)
                {
                    Log.Warning("There was more than one record found when searching for scopeId {veId}. This should not occur. The old scope records must be removed manually from Elastic.", veId);
                }
                return result.Documents.FirstOrDefault();
            }

            // Es handelt sich um einen DocKey, nachdem gesucht werden soll.
            Log.Debug("Trying to fetch ElasticRecord by doing a search with the DocKey using the elastic _id {archiveRecordId}", archiveRecordId);
            var encodedArchiveRecordId = WebUtility.UrlDecode(archiveRecordId);
            var r = Client.Search<ElasticArchiveRecord>(s =>
                s.Source(sf =>
                    {
                        return metadataToExclude switch
                        {
                            MetadataToExclude.OCRContentAndFiles => sf.Excludes(e => e.Fields("primaryData.items")),
                            MetadataToExclude.OCRContent => sf.Excludes(e => e.Fields("primaryData.items.content")),
                            MetadataToExclude.Nothing => sf,
                            _ => throw new ArgumentOutOfRangeException(nameof(metadataToExclude), metadataToExclude, null)
                        };
                    })
                    .Query(q => q
                        .Ids(sel => sel.Values(encodedArchiveRecordId))
                    ));

            if (r.Documents.Any())
            {
                return r.Documents.FirstOrDefault();
            }

            // Wurde nichts gefunden, kann es immer noch sein, dass für die Suche der neue DocKey verwendet wurde, aber der Record gar noch nicht neu synchronisiert wurde.
            // In diesem Fall müssen wir die zum DocKey gehörige scopeId holen und damit die Suche versuchen. 
            // Bringt auch dies kein Treffer, dann existiert der Datensatz nicht.
            var mappingProvider = new ActaProMappingProvider();
            var scopeId = mappingProvider.GetScopeId(archiveRecordId);
            Log.Debug("Trying to fetch ElasticRecord by doing a search with the mapped scopeId from the DocKey using the elastic _id {scopeId}", scopeId);
            r = Client.Search<ElasticArchiveRecord>(s =>
                s.Source(sf =>
                    {
                        return metadataToExclude switch
                        {
                            MetadataToExclude.OCRContentAndFiles => sf.Excludes(e => e.Fields("primaryData.items")),
                            MetadataToExclude.OCRContent => sf.Excludes(e => e.Fields("primaryData.items.content")),
                            MetadataToExclude.Nothing => sf,
                            _ => throw new ArgumentOutOfRangeException(nameof(metadataToExclude), metadataToExclude, null)
                        };
                    })
                    .Query(q => q
                        .Ids(sel => sel.Values(scopeId))
                    ));

            if (!r.Documents.Any())
            {
                Log.Information("Elastic record could not be found with passed id {archiveRecordId}", archiveRecordId);

            }

            return r.Documents.FirstOrDefault();
        }

        public ElasticArchiveDbRecord GetDbRecord(string archiveRecordIdOrSignature, MetadataToExclude metadataToExclude)
        {
            if (string.IsNullOrEmpty(archiveRecordIdOrSignature))
            {
                return null;
            }

            if (int.TryParse(archiveRecordIdOrSignature, out int veId))
            {
                // scope
                var scopeIdTranslator = new ExternalKeysQueryProvider();
                var searchRequest = new SearchRequest<ElasticArchiveDbRecord>
                {
                    Query = scopeIdTranslator.CreateExternalKeysQuery(archiveRecordIdOrSignature, "scopeArchiv"),
                    Source = new SourceFilter()
                    {
                        Excludes = GetExcludeFields(metadataToExclude)
                    }
                };
                var result = Client.Search<ElasticArchiveDbRecord>(searchRequest);

                // Back Up
                if (result.Documents.Count == 0)
                {
                    result = Client.Search<ElasticArchiveDbRecord>(s =>
                       s.Source(sf =>
                           {
                               return metadataToExclude switch
                               {
                                   MetadataToExclude.OCRContentAndFiles => sf.Excludes(e => e.Fields("primaryData.items")),
                                   MetadataToExclude.OCRContent => sf.Excludes(e => e.Fields("primaryData.items.content")),
                                   MetadataToExclude.Nothing => sf,
                                   _ => throw new ArgumentOutOfRangeException(nameof(metadataToExclude), metadataToExclude, null)
                               };
                           })
                           .Query(q => q
                               .Ids(sel => sel.Values(veId))
                           ));
                }

                //  Id is or must be unique
                if (result.Documents.Count > 1)
                {
                    Log.Warning("There was more than one record found when searching for scopeId {veId}. This should not occur. The old scope records must be removed manually from Elastic.", veId);
                }
                return result.Documents.FirstOrDefault();
            }


            // Vz      685f0872-f984-4f66-88a4-e6cd873fb1d0
            // Arch    b83a8ece-92dc-506d-9baa-4126a74c139f
            // Best    9c427a63-b945-524c-820a-c411451025d9
            // TBest   32da7073-4641-514c-a7eb-0c552e8675c7
            // TBest%20%20%2032da7073-4641-514c-a7eb-0c552e8675c7
            // Best%20%20%20%209c427a63-b945-524c-820a-c411451025d9
            // UUid is always 44 chars long
            var archiveRecordId = WebUtility.UrlDecode(archiveRecordIdOrSignature);
            if (archiveRecordId.Length == 44)
            {
                var result = Client.Search<ElasticArchiveDbRecord>(s =>
                    s.Source(sf =>
                        {
                            return metadataToExclude switch
                            {
                                MetadataToExclude.OCRContentAndFiles => sf.Excludes(e => e.Fields("primaryData.items")),
                                MetadataToExclude.OCRContent => sf.Excludes(e => e.Fields("primaryData.items.content")),
                                MetadataToExclude.Nothing => sf,
                                _ => throw new ArgumentOutOfRangeException(nameof(metadataToExclude), metadataToExclude, null)
                            };
                        })
                        .Query(q => q
                            .Ids(sel => sel.Values(archiveRecordId))
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

            var searchResponse = GetRecord(id, MetadataToExclude.OCRContentAndFiles);

            var retryCount = 0;

            // Manchmal ist der Index noch nicht aktuell, deshalb versuchen wir es bis zu 5 mal
            // mit einer Sekunde Pause dazwischen.
            // Das ist vor allem nach einer Neusynchronisation eines Records der Fall.
            while (retryCount < 5 && (searchResponse == null || searchResponse.ArchiveRecordId != id))
            {
                retryCount++;
                Task.Delay(1000);
                searchResponse = GetRecord(id, MetadataToExclude.OCRContentAndFiles);
            }

            if (searchResponse == null)
            {
                Log.Warning("Konnte die Tokens nicht aktualisieren für id {id}, weil Index Record nicht gefunden.", id);
                return;
            }

            if (retryCount > 0)
            {
                Log.Information("Der Index Record für id {id} wurde erst nach {retryCount} Versuchen gefunden.", id, retryCount);
            }

            // Für das MetadataAccessToken muss mindestens ein Token (für BAR) geliefert werden.
            // Ansonsten stimmt etwas nicht und wir brechen den Update ab. Null dürfen sie auch bei allen nicht sein
            // Ausser für FieldAccessTokens, dann werden keine
            // individuellen FieldAccessToken hinterlegt. Nur die VE's gemäss Art. 12.3 anonymisiert werden bzw. geschützt sind, benötigen FieldAccessToken 
            if (primaryDataDownloadAccessTokens == null || primaryDataFulltextAccessTokens == null || metadataAccessTokens == null ||
                metadataAccessTokens.Length == 0)
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

        private string[] GetExcludeFields(MetadataToExclude metadataToExclude)
        {
            switch (metadataToExclude)
            {
                case MetadataToExclude.OCRContentAndFiles:
                    return ["primaryData.items"];
                case MetadataToExclude.OCRContent:
                    return ["primaryData.items.content"];
                case MetadataToExclude.Nothing:
                    return [];
                default:
                    throw new ArgumentOutOfRangeException(nameof(metadataToExclude), metadataToExclude, null);
            }
        }
    }
}
