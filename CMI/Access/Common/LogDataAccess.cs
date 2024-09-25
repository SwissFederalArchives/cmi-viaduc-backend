using System;
using System.Collections.Generic;
using System.Linq;
using CMI.Access.Common.Properties;
using CMI.Contract.Common;
using CMI.Utilities.Common.Helpers;
using Elasticsearch.Net;
using Nest;
using Nest.JsonNetSerializer;
using Newtonsoft.Json.Converters;
using Serilog;


namespace CMI.Access.Common
{
    public class LogDataAccess : ILogDataAccess
    {
        private readonly ElasticClient client;
        private readonly string indexName;


        public LogDataAccess() : this(Settings.Default.ElasticSearchUrl, Settings.Default.ElasticSearchUsername, Settings.Default.ElasticSearchPWD)
        {
        }

        public LogDataAccess(string elasticUri, string elasticusername, string elasticpwd, string indexName = "logstash-*")
        {
            var pool = new SingleNodeConnectionPool(new Uri(elasticUri));
            var settings = new ConnectionSettings(pool, (serializer, values) => new JsonNetSerializer(
                serializer, values, null, null,
                new[] {new ExpandoObjectConverter()}));

            this.indexName = indexName;
            settings.DefaultIndex(indexName);
            if (!string.IsNullOrEmpty(elasticusername))
            {
                settings.BasicAuthentication(elasticusername, elasticpwd);
            }
            settings.ThrowExceptions();
            client = new ElasticClient(settings);
        }


        public IList<ElasticRawLogRecord> GetLogData(LogDataFilter filter)
        {
            var takeNumber = 10000;
            var scrollTimeout = new Time("1m");
            var retVal = new List<ElasticRawLogRecord>();

            var query = BuildQuery(filter);
            var initialResponse = client.Search<ElasticRawLogRecord>(s => s
                .Query(q => query)
                .Sort(sort => sort.Ascending($"@{nameof(ElasticRawLogRecord.Timestamp).ToLowerCamelCase()}"))
                .From(0)
                .Take(takeNumber)
                .Scroll(scrollTimeout)
            );

            // Add the first batch to the result
            retVal.AddRange(GetLogRecords(initialResponse));

            // Get the rest of the data
            var scrollid = initialResponse.ScrollId;
            var isScrollSetHasData = true;
            while (isScrollSetHasData)
            {
                var loopingResponse = client.Scroll<ElasticRawLogRecord>(scrollTimeout, scrollid);
                if (loopingResponse.IsValid)
                {
                    retVal.AddRange(GetLogRecords(loopingResponse));
                    scrollid = loopingResponse.ScrollId;
                }

                isScrollSetHasData = loopingResponse.Documents.Any();
            }

            client.ClearScroll(new ClearScrollRequest(scrollid));


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
                var concreteIndexName = index.Key.Name;
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
                timestampQuery = new DateRangeQuery
                {
                    Field = $"{nameof(ElasticRawLogRecord.Timestamp)}",
                    GreaterThanOrEqualTo = new DateMathExpression(filter.StartDate.Value),
                    LessThanOrEqualTo = new DateMathExpression(filter.EndDate.Value)
                };
            }

            var query = new BoolQuery();
            if (timestampQuery != null)
            {
                query.Filter = new QueryContainer[] {timestampQuery};
            }

            return query;
        }

        private List<ElasticRawLogRecord> GetLogRecords(ISearchResponse<ElasticRawLogRecord> data)
        {
            // Need to push the id and index name into the log record as these properties are not automatically deserialized
            var result = data.Hits.Select(h =>
            {
                h.Source.Id = h.Id;
                h.Source.Index = h.Index;
                return h.Source;
            }).ToList();
            return result;
        }
    }
}
