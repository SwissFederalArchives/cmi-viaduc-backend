using CMI.Access.Common;
using CMI.Contract.Common;
using CMI.Contract.Parameter;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace CMI.Manager.Index
{
    public class ElasticLogManager : IElasticLogManager
    {
        private readonly ILogDataAccess logDataAccess;
        private readonly IParameterHelper parameterHelper;


        public ElasticLogManager(ILogDataAccess logDataAccess, IParameterHelper parameterHelper)
        {
            this.logDataAccess = logDataAccess;
            this.parameterHelper = parameterHelper;
        }

        public async Task<GetElasticLogRecordsResult> GetElasticLogRecords(LogDataFilter filter)
        {
            var sw = new Stopwatch();
            sw.Start();
            var raw = await logDataAccess.GetLogData(filter);
            var result = new GetElasticLogRecordsResult
            {
                Records = raw.Select(ToElasticLogRecord).ToList(),
                TotalCount = raw.Count
            };
            // Stop execution time including conversion of record
            result.ExecutionTime = sw.Elapsed;

            return result;
        }

        public void DeleteOldLogIndexes()
        {
            try
            {
                var aufbewahrungsdauer = parameterHelper.GetSetting<IndexSettings>().AufbewahrungsdauerLogIndex;

                logDataAccess.DeleteLogIndexes(DateTime.Now.AddDays(-aufbewahrungsdauer));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during task 'delete old log indexes'");
            }
        }


        /// <summary>
        /// Normalizes an Elasticsearch date value to local time.
        /// If the dateKind is unspecified then it is assumed to be in UTC. When the JSON deserializer produces a
        /// <see cref="DateTimeKind.Unspecified"/> value (i.e., no timezone suffix was present in
        /// the raw JSON), the datetime is treated as UTC before converting to local time.
        /// </summary>
        internal static DateTime NormalizeElasticsearchTimestampToLocal(DateTime timestamp)
        {
            if (timestamp.Kind == DateTimeKind.Unspecified)
            {
                // Elasticsearch @timestamp values are always UTC; treat Unspecified accordingly.
                timestamp = DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
            }

            return timestamp.ToLocalTime();
        }

        private ElasticLogRecord ToElasticLogRecord(ElasticRawLogRecord record)
        {
            try
            {
                var timestamp = NormalizeElasticsearchTimestampToLocal(record.Timestamp);

                var retVal = new ElasticLogRecord
                {
                    Id = record.Id,
                    Index = record.Index,
                    Exception = record.Exception,
                    Timestamp = timestamp,
                    Level = record.Level,
                    MessageTemplate = record.MessageTemplate,
                    Message = ReplaceTemplate(record.MessageTemplate, record.Properties),
                    ArchiveRecordId = record.Properties.ContainsKey(nameof(ElasticLogRecord.ArchiveRecordId)) ? record.Properties[nameof(ElasticLogRecord.ArchiveRecordId)].GetString() :
                           record.Properties.ContainsKey("archiveRecordIdOrSignature") ? record.Properties["archiveRecordIdOrSignature"].GetString() : string.Empty,
                    ConversationId = record.Properties.ContainsKey(nameof(ElasticLogRecord.ConversationId)) ? record.Properties[nameof(ElasticLogRecord.ConversationId)].GetString() : string.Empty,
                    MainAssembly = record.Properties[nameof(ElasticLogRecord.MainAssembly)].GetString(),
                    MachineName = record.Properties[nameof(ElasticLogRecord.MachineName)].GetString(),
                    ProcessId = record.Properties[nameof(ElasticLogRecord.ProcessId)].GetInt64(),
                    ThreadId = record.Properties[nameof(ElasticLogRecord.ThreadId)].GetInt64()
                };
                return retVal;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unexpected error in {methodName}", nameof(ToElasticLogRecord));
                throw;
            }
        }

        public static string ReplaceTemplate(string recordMessageTemplate, Dictionary<string, JsonElement> values)
        {
            foreach (var kvp in values)
            {
                recordMessageTemplate = recordMessageTemplate.Replace($"{{{kvp.Key}}}", kvp.ToString());
            }

            return recordMessageTemplate;
        }

    }
}