using System;
using System.Diagnostics;
using System.Dynamic;
using System.Linq;
using System.Text.RegularExpressions;
using CMI.Access.Common;
using CMI.Contract.Common;
using CMI.Contract.Parameter;
using Serilog;

namespace CMI.Manager.Index
{
    public class ElasticLogManager : IElasticLogManager
    {
        private readonly LogDataAccess logDataAccess;
        private readonly IParameterHelper parameterHelper;


        public ElasticLogManager(LogDataAccess logDataAccess, IParameterHelper parameterHelper)
        {
            this.logDataAccess = logDataAccess;
            this.parameterHelper = parameterHelper;
        }

        public GetElasticLogRecordsResult GetElasticLogRecords(LogDataFilter filter)
        {
            var sw = new Stopwatch();
            sw.Start();
            var raw = logDataAccess.GetLogData(filter);
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


        private ElasticLogRecord ToElasticLogRecord(ElasticRawLogRecord record)
        {
            var retVal = new ElasticLogRecord
            {
                Id = record.Id,
                Index = record.Index,
                Exception = record.Exception,
                Timestamp = record.Timestamp,
                Level = record.Level,
                MessageTemplate = record.MessageTemplate,
                Message = ConvertMessageTemplate(record.MessageTemplate, record.Properties),
                ArchiveRecordId = GetProperty<string>(record.Properties, nameof(ElasticLogRecord.ArchiveRecordId)),
                ConversationId = GetProperty<string>(record.Properties, nameof(ElasticLogRecord.ConversationId)),
                MainAssembly = GetProperty<string>(record.Properties, nameof(ElasticLogRecord.MainAssembly)),
                MachineName = GetProperty<string>(record.Properties, nameof(ElasticLogRecord.MachineName)),
                ProcessId = GetProperty<long>(record.Properties, nameof(ElasticLogRecord.ProcessId)),
                ThreadId = GetProperty<long>(record.Properties, nameof(ElasticLogRecord.ThreadId))
            };
            return retVal;
        }

        private string ConvertMessageTemplate(string recordMessageTemplate, ExpandoObject props)
        {
            var pattern = @".*?\{(?<name>.*?)(\:(?<format>.*?))?\}.*?";
            var input = recordMessageTemplate;

            foreach (Match m in Regex.Matches(input, pattern, RegexOptions.Multiline))
            foreach (Capture capture in m.Groups["name"].Captures)
            {
                var format = m.Groups["format"].Success ? m.Groups["format"].Captures[0] : null;
                input = input.Replace($"{{{capture.Value + (format != null ? ":" + format.Value : "")}}}",
                    GetProperty<string>(props, capture.Value, format?.Value));
            }

            return input;
        }

        private T GetProperty<T>(ExpandoObject props, string name, string formatString = null)
        {
            var obj = props.FirstOrDefault(p => p.Key.Equals(name, StringComparison.InvariantCultureIgnoreCase)).Value;
            if (obj != null)
            {
                if (typeof(T) == typeof(string))
                {
                    if (string.IsNullOrEmpty(formatString))
                    {
                        return (T) (object) obj.ToString();
                    }
                    else
                    {
                        var template = "{0:" + formatString + "}";
                        return (T) (object) string.Format(template, obj);
                    }
                }

                return (T) obj;
            }

            return default;
        }
    }
}