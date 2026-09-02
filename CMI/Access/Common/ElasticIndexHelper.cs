using CMI.Contract.Common;
using CMI.Utilities.ActaPro;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.IndexManagement;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace CMI.Access.Common
{
    public class ElasticIndexHelper
    {
        private readonly string index;

        public ElasticIndexHelper(Uri elasticUri, string userName = "", string pwd = "", string indexName = "archive")
        {
            var settings = new ElasticsearchClientSettings(elasticUri)
                .DefaultMappingFor<ElasticArchiveRecord>(m => m.IndexName(indexName))
                .DefaultMappingFor<ElasticArchiveDbRecord>(m => m.IndexName(indexName));

            index = indexName;

            if (!string.IsNullOrEmpty(userName))
            {
                settings.Authentication(new BasicAuthentication(userName, pwd));
            }
            settings.DefaultIndex(indexName);
            settings.ThrowExceptions();
#if DEBUG
            settings.DisableDirectStreaming(); // Zum Debuggen aktivieren, damit man die Requests und Responses sieht.
#endif

            Client = new ElasticsearchClient(settings);
        }


        public ElasticIndexHelper(ElasticsearchClient client, string indexName)
        {
            Client = client;
            index = indexName;
        }

        public ElasticIndexHelper()
        {
        }

        public ElasticsearchClient Client { get; }

        public long CountDocuments
        {
            get
            {
                Client.Indices.Refresh(new RefreshRequest(index));
                var response = Client.Count<ElasticArchiveRecord>(r => r.Indices(index));

                if (!response.IsSuccess())
                {
                    throw new InvalidOperationException(
                        $"Fehler beim Zählen der Dokumente: {response.DebugInformation}");
                }

                return response.Count;
            }
        }

        public async Task CreateIndex(string indexName)
        {
            string json;
            var assembly = GetType().Assembly;
            var resourceName = "CMI.Access.Common.ElasticRecordMapping.json";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream ?? throw new InvalidOperationException()))
            {
                json = await reader.ReadToEndAsync();
            }

            var result = await Client.Transport.RequestAsync<StringResponse>(
                HttpMethod.PUT, 
                index, 
                PostData.String(json));

            if (!result.ApiCallDetails.HasSuccessfulStatusCode)
            {
                throw new InvalidOperationException(
                    $"Fehler beim Erstellen des Index '{indexName}': {result.Body}");
            }
        }

        public async Task<bool> IndexExists(string indexName)
        {
            // ExistsAsync prüft intern sowohl Indizes als auch Aliase
            var response = await Client.Indices.ExistsAsync(indexName);
            return response.Exists;
        }

        public async Task DeleteIndex(string indexName)
        {
            var response = await Client.Indices.DeleteAsync(indexName);

            if (!response.Acknowledged)
            {
                throw new Exception("Delete not acknowledged");
            }
        }

        public async Task Index(ElasticArchiveRecord record)
        {
            Log.Debug(
                "Writing the record into the elastic index. ArchivRecordId: {archiveRecordId}. Callstack is: {callstack}",
                record.ArchiveRecordId,
                Environment.StackTrace);

            IndexResponse response;
            if (record is ElasticArchiveDbRecord)
            {
                response = await Client.IndexAsync (
                    record as ElasticArchiveDbRecord,
                    i => i.Id(record.ArchiveRecordId)
                        .Refresh(Refresh.WaitFor));  // Wartet bis der Refresh abgeschlossen ist; 

            }
            else
            { 
                response = await Client.IndexAsync (
                    record,
                    i => i.Id(record.ArchiveRecordId)
                        .Refresh(Refresh.WaitFor)); // Wartet bis der Refresh abgeschlossen ist; 
            }

            if (!response.IsSuccess())
            {
                Log.Error("Problem beim Indexieren des Records für archivRecordId: {archiveRecordId}. Response: {response}", record.ArchiveRecordId, JsonSerializer.Serialize(response));
                throw new InvalidOperationException($"Problem beim Indexieren des Records für archivRecordId: {record.ArchiveRecordId}. Response: {response}");
            }
            
            Log.Information("Successfully updated the data in elastic index for archivRecordId: {archiveRecordId}. Response valid: {IsValidResponse}, {result}, DebugInfo: {debug}",
                record.ArchiveRecordId,
                response.IsSuccess(), response.Result.ToString(), response.DebugInformation);
        }

        public async Task IndexBulk(IEnumerable<ElasticArchiveRecord> records)
        {
            var operations = records.Select(r => new BulkIndexOperation<ElasticArchiveRecord>(r) {Id = r.ArchiveRecordId}).Cast<IBulkOperation>().ToList();

            var request = new BulkRequest
            {
                Operations = operations
            };

            var response = await Client.BulkAsync(request);

            if (response.Errors)
            {
                var failedItems = response.ItemsWithErrors
                    .Select(i => $"Id: {i.Id}, Error: {i.Error?.Reason}")
                    .ToList();

                Log.Error(
                    "Fehler beim Bulk-Indexieren. Fehlgeschlagene Records: {failedItems}",
                    string.Join(", ", failedItems));

                throw new InvalidOperationException(
                    $"Bulk-Indexierung fehlgeschlagen für {failedItems.Count} Records: {string.Join(", ", failedItems)}");
            }

            Log.Information("Bulk-Indexierung erfolgreich. {count} Records indexiert.", operations.Count);
        }


        public async Task Remove(string archiveRecordId)
        {
            Log.Information("Delete Command for Record with archiveRecordId: {archiveRecordId}", archiveRecordId);
            // Let's check if the record we want to delete is available.
            var record = await GetRecord(archiveRecordId, MetadataToExclude.OCRContentAndFiles);
            if (record != null)
            {
                Log.Information("Found Record to delete with archiveRecordId: {archiveRecordId}", record.ArchiveRecordId);
                var response = await Client.DeleteAsync<ElasticArchiveRecord>(record.ArchiveRecordId);
                if (response.IsSuccess())
                {
                    Log.Information("Record delete command executed successfully with the archiveRecordId: {archiveRecordId}", record.ArchiveRecordId);
                }
                else
                {
                    Log.Warning(
                        "Delete failed for archiveRecordId: {archiveRecordId}. Server error: {serverError}. Debug information: {debugInformation}",
                        record.ArchiveRecordId,
                        response.ElasticsearchServerError,
                        response.DebugInformation);
                }
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
        public async Task<ElasticArchiveRecord> GetRecord(string archiveRecordId, MetadataToExclude metadataToExclude)
        {
            if (string.IsNullOrEmpty(archiveRecordId))
            {
                return null;
            }

            var sourceFilter = GetSourceFilter(metadataToExclude);
            
            if (int.TryParse(archiveRecordId, out var veId))
            {
                // Es wird versucht die scopeId über eine Suche nach dem ExternalKey zu finden.
                // Nachdem ein Record neu synchronisiert ist, ist die scopeId nur noch im ExternalKey zu finden.
                Log.Debug("Trying to fetch ElasticRecord by doing a search with the scopeId using the externalKey {veId}", veId);
                var scopeIdTranslator = new ExternalKeysQueryProvider();
                var searchRequest = new SearchRequest<ElasticArchiveRecord>
                {
                    Query = scopeIdTranslator.CreateExternalKeysQuery(archiveRecordId, "scopeArchiv"),
                    Source = new SourceConfig(sourceFilter)
                };
                var result = await Client.SearchAsync<ElasticArchiveRecord>(searchRequest);

                if (!result.IsSuccess())
                {
                    throw new InvalidOperationException($"Fehler beim Suchen nach ExternalKey von scopeArchiv {veId}: {result.DebugInformation}");
                }

                // Backup: Suche anhand der Elastic _id (PrimaryKey / veId)
                // Nötig wenn Record noch nie neu synchronisiert wurde
                if (result.Documents.Count == 0)
                {
                    Log.Debug("Trying to fetch ElasticRecord by doing a search with the scopeId using the elastic _id {veId}", veId);
                    return await GetRecordById<ElasticArchiveRecord>(veId.ToString(), sourceFilter);
                }

                if (result.Documents.Count > 1)
                {
                    Log.Warning("There was more than one record found when searching for scopeId {veId}. This should not occur. The old scope records must be removed manually from Elastic.", veId);
                }

                return result.Documents.FirstOrDefault();
            }

            // Es handelt sich um einen DocKey
            Log.Debug("Trying to fetch ElasticRecord by doing a search with the DocKey using the elastic _id {archiveRecordId}", archiveRecordId);

            archiveRecordId = WebUtility.UrlDecode(archiveRecordId);
            
            var docKeyRecord = await GetRecordById<ElasticArchiveRecord>(archiveRecordId, sourceFilter);  
            if (docKeyRecord != null)
            {
                return docKeyRecord;
            }

            // Fallback: DocKey → scopeId mapping (Record noch nie synchronisiert)
            var mappingProvider = new ActaProMappingProvider();
            var scopeId = mappingProvider.GetScopeId(archiveRecordId);
            Log.Debug("Trying to fetch ElasticRecord by doing a search with the mapped scopeId from the DocKey using the elastic _id {scopeId}", scopeId);
            return await GetRecordById<ElasticArchiveRecord>(scopeId.ToString(), sourceFilter);

        }

       public async Task<ElasticArchiveDbRecord> GetDbRecord(string archiveRecordIdOrSignature, MetadataToExclude metadataToExclude)
        {
            if (string.IsNullOrEmpty(archiveRecordIdOrSignature))
            {
                return null;
            }

            var sourceFilter = new SourceFilter();
            switch (metadataToExclude)
            {
                case MetadataToExclude.OCRContentAndFiles:
                    sourceFilter.Excludes = Infer.Fields("primaryData.items");
                    break;
                case MetadataToExclude.OCRContent:
                    sourceFilter.Excludes = Infer.Fields("primaryData.items.content");
                    break;
            }

            if (int.TryParse(archiveRecordIdOrSignature, out var veId))
            {
                // Es wird versucht die scopeId über eine Suche nach dem ExternalKey zu finden.
                // Nachdem ein Record neu synchronisiert ist, ist die scopeId nur noch im ExternalKey zu finden.
                Log.Debug("Trying to fetch ElasticDbRecord by doing a search with the scopeId using the externalKey {veId}", veId);
                var scopeIdTranslator = new ExternalKeysQueryProvider();
                var searchRequest = new SearchRequest<ElasticArchiveDbRecord>
                {
                    Query = scopeIdTranslator.CreateExternalKeysQuery(archiveRecordIdOrSignature, "scopeArchiv"),
                    Source = new SourceConfig(sourceFilter)
                };

                var result = await Client.SearchAsync<ElasticArchiveDbRecord>(searchRequest);

                if (!result.IsSuccess())
                {
                    throw new InvalidOperationException($"Fehler beim Suchen nach ExternalKey von scopeArchiv {veId}: {result.DebugInformation}");
                }

                // Backup: Suche anhand der Elastic _id (PrimaryKey / veId)
                // Nötig wenn Record noch nie neu synchronisiert wurde
                if (result.Documents.Count == 0)
                {
                    Log.Debug("Trying to fetch ElasticRecord by doing a search with the scopeId using the elastic _id {veId}", veId);
                    return await GetRecordById<ElasticArchiveDbRecord>(veId.ToString(), sourceFilter);
                }

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
                var docKeyRecord = await GetRecordById<ElasticArchiveDbRecord>(archiveRecordId, sourceFilter);
                if (docKeyRecord != null)
                {
                    return docKeyRecord;
                }

                // Fallback: DocKey → scopeId mapping (Record noch nie synchronisiert)
                var mappingProvider = new ActaProMappingProvider();
                var scopeId = mappingProvider.GetScopeId(archiveRecordId);
                Log.Debug("Trying to fetch ElasticRecord by doing a search with the mapped scopeId from the DocKey using the elastic _id {scopeId}", scopeId);
                return await GetRecordById<ElasticArchiveDbRecord>(scopeId.ToString(), sourceFilter);
            }
            else
            {
                var searchRequest = new SearchRequest<ElasticArchiveDbRecord> { Query = CreateQueryForSignatur(archiveRecordIdOrSignature) };
                var result = await Client.SearchAsync<ElasticArchiveDbRecord>(searchRequest);
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

        public async Task RemoveAll()
        {
            await Client.DeleteByQueryAsync<ElasticArchiveRecord>(q => q
                .Indices(index)
                .Query(rq => rq
                    .MatchAll(new MatchAllQuery())));
        }

        public async Task UpdateTokens(string id, string[] primaryDataDownloadAccessTokens, string[] primaryDataFulltextAccessTokens,
            string[] metadataAccessTokens, string[] fieldAccessTokens)
        {
            var elasticArchiveRecord = await GetRecord(id, MetadataToExclude.OCRContentAndFiles);

            // Da beim Indexieren jetzt Refresh.WaitFor verwendet wird und GetRecord einen Record neu innerhalb von GetRecord mit GetAsync<T> geholt wird,
            // sollte dieser Retry-Loop in der Praxis nie mehr als 1 Iteration benötigen. Er kann aber sicherheitshalber drin bleiben.
            // Manchmal ist der Index noch nicht aktuell, deshalb versuchen wir es bis zu 5 mal
            // mit einer Sekunde Pause dazwischen.
            // Das ist vor allem nach einer Neusynchronisation eines Records der Fall.
            var retryCount = 0;
            while (retryCount < 5 && (elasticArchiveRecord == null || elasticArchiveRecord.ArchiveRecordId != id))
            {
                retryCount++;
                await Task.Delay(1000);
                elasticArchiveRecord = await GetRecord(id, MetadataToExclude.OCRContentAndFiles);
            }

            if (elasticArchiveRecord == null)
            {
               Log.Warning("Beim Versuch die Tokens zu aktualisieren, konnte der entsprechende Datensatz mit Id {id} nicht gefunden werden.", id);
               throw new InvalidOperationException($"Beim Versuch die Tokens zu aktualisieren, konnte der entsprechende Datensatz mit Id {id} nicht gefunden werden.");
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

            var updateResponse = await Client.UpdateAsync<ElasticArchiveRecord, object>(index, id, u =>
                u.Doc(new
                {
                    PrimaryDataDownloadAccessTokens = primaryDataDownloadAccessTokens.ToList(),
                    PrimaryDataFulltextAccessTokens = primaryDataFulltextAccessTokens.ToList(),
                    MetadataAccessTokens = metadataAccessTokens.ToList(),
                    FieldAccessTokens = fieldAccessTokens?.ToList()
                })
            );

            if (!updateResponse.IsSuccess())
            {
                Log.Error("Problem beim Update der AccessTokens im Index für Id {id}. updateResponse={response}", id, updateResponse);
                throw new InvalidOperationException($"Problem beim Update der AccessTokens im Index für Id {id}. updateResponse={updateResponse}");
            }
        }

        public async Task<ElasticTestResponse> GetIndexHealth()
        {
            var aliasResponse = await Client.Indices.GetAliasAsync(new GetAliasRequest(Indices.Index(index)));

            if (!aliasResponse.IsValidResponse)
            {
                Log.Warning("Fehler beim Lesen der Index-Settings für {index}: {info}", index, aliasResponse.DebugInformation);
                throw new Exception($"Fehler beim Lesen der Index-Settings für {index}: {aliasResponse.DebugInformation}");
            }

            if (aliasResponse.Aliases.Count != 1)
            {
                Log.Warning("{count} Indices haben den Alias \"{index}\".", aliasResponse.Aliases.Count, index);
                throw new Exception($"{aliasResponse.Aliases.Count} Indices haben den Alias \"{index}\".");
            }

            var indexName = aliasResponse.Aliases.FirstOrDefault().Key;
            
            var statsResponse = await Client.Indices.StatsAsync(i => i.Indices(indexName));
            var settingsResponse = await Client.Indices.GetSettingsAsync(new GetIndicesSettingsRequest(Indices.Index(indexName)));

            var indexStats = statsResponse.Indices[indexName];
            var indexSettings = settingsResponse.Settings[indexName].Settings?.Index;
            if (!statsResponse.IsValidResponse && indexStats == null)
            {
                Log.Warning("Fehler beim Lesen der Index-Stats für {index}: {info}", indexName, statsResponse.DebugInformation);
                throw new Exception($"Fehler beim Lesen der Index-Settings für {index}: {statsResponse.DebugInformation}");
            }
            if (!settingsResponse.IsValidResponse && indexSettings == null)
            {
                Log.Warning("Fehler beim Lesen der Index-Settings für {index}: {info}", indexName, settingsResponse.DebugInformation);
                throw new Exception($"Fehler beim Lesen der Index-Settings für {index}: {settingsResponse.DebugInformation}");
            }
            return new ElasticTestResponse
            {
                IsReadOnly = indexSettings?.Blocks?.ReadOnlyAllowDelete ?? false,
                DocsCount = indexStats.Primaries?.Docs?.Count.ToString(),
                Health = indexStats.Health?.ToString().ToLower(),
                Status = indexStats.Status?.ToString().ToLower()
            };
        }

        /// <summary>
        /// Finds a record in the index using its _id.
        /// if the record is not found , null is returned. If there is an error during the search, an exception is thrown.
        /// </summary>
        /// <param name="archiveRecordId"></param>
        /// <param name="sourceFilter"></param>
        /// <returns>The record, or null if not found.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        private async Task<T> GetRecordById<T>(string archiveRecordId, SourceFilter sourceFilter) where T : class
        {
            var idRequest = new GetRequest(index, archiveRecordId)
            {
                SourceExcludes = sourceFilter.Excludes,
            };

            // GetAsync  — direkt via _id, kein Refresh nötig, immer aktuell
            var getResponse = await Client.GetAsync<T>(idRequest);

            if (!getResponse.IsSuccess() && getResponse.ApiCallDetails.HttpStatusCode != 404)
                throw new InvalidOperationException($"Fehler beim Lesen von _id {archiveRecordId}: {getResponse.DebugInformation}");

            if (getResponse.Found)
            {
                return getResponse.Source;
            }

            return null;
        }

        // Nur zum Testen
        public async Task SetIndexReadOnly(bool readOnly)
        {
            var body = $@"{{
                    ""index"": {{
                        ""blocks"": {{
                            ""read_only_allow_delete"": ""{readOnly.ToString().ToLower()}""
                        }}
                    }}
                }}";

            var response = await Client.Transport.RequestAsync<StringResponse>(
                HttpMethod.PUT,
                $"/{index}/_settings",
                PostData.String(body));

            if (!response.ApiCallDetails.HasSuccessfulStatusCode)
            {
                throw new InvalidOperationException(
                    $"Fehler beim Setzen von ReadOnly={readOnly} auf Index '{index}': {response.ApiCallDetails.DebugInformation}");
            }
        }


        /// <summary>
        /// is copied from CMI.Web.Frontend.api.Search public static class ElasticQueryBuilder
        /// </summary>
        /// <param name="signatur"></param>
        /// <returns></returns>
        private static Query CreateQueryForSignatur(string signatur)
        {
            var boolQuery = new BoolQuery
            {
                Must =
                [
                    new TermQuery(new Field( "referenceCode"),FieldValue.String(signatur))
                ]
            };
            return boolQuery;
        }

        private SourceFilter GetSourceFilter(MetadataToExclude metadataToExclude)
        {
            var sourceFilter = new SourceFilter();
            switch (metadataToExclude)
            {
                case MetadataToExclude.OCRContentAndFiles:
                    sourceFilter.Excludes = Infer.Fields("primaryData.items");
                    break;
                case MetadataToExclude.OCRContent:
                    sourceFilter.Excludes = Infer.Fields("primaryData.items.content");
                    break;
            }
            return sourceFilter;
        }
    }
}
