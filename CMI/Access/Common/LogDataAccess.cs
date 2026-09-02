using CMI.Access.Common.Properties;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Core.Search;
using Elastic.Clients.Elasticsearch.QueryDsl;
using Elastic.Transport;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace CMI.Access.Common
{
    public class LogDataAccess : ILogDataAccess
    {
        private readonly ElasticsearchClient client;
        private readonly string indexName;


        public LogDataAccess() : this(Settings.Default.ElasticSearchUrl, Settings.Default.ElasticSearchUsername, Settings.Default.ElasticSearchPWD)
        {
        }

        public LogDataAccess(string elasticUri, string elasticusername, string elasticpwd, string indexName = "logstash-*") 
        {
            var settings = new ElasticsearchClientSettings(new Uri(elasticUri));
            this.indexName = indexName;
            settings.DefaultIndex(indexName);
            if (!string.IsNullOrEmpty(elasticusername))
            {
                settings.Authentication(new BasicAuthentication(elasticusername, elasticpwd));
            }
            settings.ThrowExceptions();
            client = new ElasticsearchClient(settings);
        }



        public async Task<IList<ElasticRawLogRecord>> GetLogData(LogDataFilter filter)
        {
            const int takeNumber = 10000;
            const string keepAlive = "1m";
            var retVal = new List<ElasticRawLogRecord>();
            var query = BuildQuery(filter);
            var sortField = $"@{nameof(ElasticRawLogRecord.Timestamp).ToLowerCamelCase()}";

            // PIT öffnen
            var pitResponse = await client.OpenPointInTimeAsync(
                indexName,
                p => p.KeepAlive(new Duration(keepAlive)));

            if (!pitResponse.IsSuccess())
            {
                Log.Error("Fehler beim Öffnen des Point-in-Time für GetLogData: {info}", pitResponse.DebugInformation);
                return retVal;
            }

            var pitId = pitResponse.Id;
            ICollection<FieldValue> searchAfter = null;

            try
            {
                while (true)
                {
                    var searchRequest = new SearchRequest<ElasticRawLogRecord>
                    {
                        Query = query,
                        Size = takeNumber,
                        Sort =
                        [
                            new SortOptions
                            {
                                Field = new FieldSort(new Field(sortField)) {Order = SortOrder.Asc}
                            }
                        ],
                        Pit = new PointInTimeReference(pitId) {KeepAlive = new Duration(keepAlive)},
                        SearchAfter = searchAfter,
                        // Index darf bei PIT nicht gesetzt werden — wird vom PIT übernommen
                    };

                    var response = await client.SearchAsync<ElasticRawLogRecord>(searchRequest);

                    if (!response.IsSuccess())
                    {
                        Log.Error("Fehler beim Abrufen von Log-Daten: {info}", response.DebugInformation);
                        break;
                    }

                    if (response.Hits.Count == 0)
                        break;

                    retVal.AddRange(GetLogRecords(response));

                    // PIT-ID aktualisieren (kann sich pro Request ändern)
                    pitId = response.PitId ?? pitId;

                    // search_after für nächste Seite setzen
                    searchAfter = response.HitsMetadata.Hits.Last().Sort?.ToList();

                    // Falls keine Sort-Werte vorhanden, dann können wir nicht weiter paginieren
                    if (searchAfter == null || searchAfter.Count == 0)
                        break;
                }
            }
            finally
            {
                // PIT immer schliessen, auch bei Fehler
                var closeResponse = await client.ClosePointInTimeAsync(r => r.Id(pitId));
                if (!closeResponse.IsSuccess())
                {
                    Log.Warning("Fehler beim Schliessen des Point-in-Time: {info}", closeResponse.DebugInformation);
                }
            }

            return retVal;
        }

        public void DeleteLogIndexes(DateTime olderThanDate)
        {
            Log.Information("Task 'delete old log indexes' started for index name {IndexName}...", indexName);

            var response = client.Indices.Get(indexName, o => o.IncludeDefaults(false));
            var staticPart = indexName.Replace("*", string.Empty);
            var deleted = 0;

            foreach (var index in response.Indices)
            {
                var concreteIndexName = index.Key;
                var datePart = concreteIndexName.Replace(staticPart, string.Empty);
                var split = datePart.Split('.');
                DateTime date;

                try
                {
                    date = new DateTime(Convert.ToInt32(split[0]), Convert.ToInt32(split[1]), Convert.ToInt32(split[2]));
                }
                catch (Exception ex) when (ex is IndexOutOfRangeException || ex is ArgumentOutOfRangeException || ex is FormatException ||
                                           ex is OverflowException)
                {
                    Log.Warning("Concrete index name {ConcreteIndexName} does not contain date in expected format.", concreteIndexName);
                    continue;
                }

                if (date.Date < olderThanDate.Date)
                {
                    client.Indices.Delete(concreteIndexName);
                    deleted++;
                }
            }

            Log.Information(
                "Task 'delete old log indexes' finished. Found {Found} concrete indexes with pattern {Pattern}. Deleted {Deleted} indexes.",
                response.Indices.Count, indexName, deleted);
        }


        private static BoolQuery BuildQuery(LogDataFilter filter)
        {
            DateRangeQuery timestampQuery = null;
            if (filter.StartDate.HasValue && filter.EndDate.HasValue)
            {
                timestampQuery = new DateRangeQuery(new Field($"{nameof(ElasticRawLogRecord.Timestamp)}"))
                {
                    Gt = new DateMathExpression(filter.StartDate.Value),
                    Lte = new DateMathExpression(filter.EndDate.Value)
                };
            }

            var query = new BoolQuery();
            if (timestampQuery != null)
            {
                query.Filter = new List<Query> { timestampQuery };
            }

            return query;
        }

        private List<ElasticRawLogRecord> GetLogRecords(SearchResponse<ElasticRawLogRecord> data)
        {
            return data.Hits
                .Where(h => h.Source != null)
                .Select(h =>
                {
                    var record = h.Source;
                    record.Id = h.Id;
                    record.Index = h.Index;
                    return record;
                })
                .ToList();
        }
    }
}
