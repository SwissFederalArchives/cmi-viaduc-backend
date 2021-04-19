using System;

namespace CMI.Contract.Common
{
    public class ElasticLogRecord
    {
        public string Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Exception { get; set; }
        public string Level { get; set; }
        public string Message { get; set; }
        public string MessageTemplate { get; set; }
        public string Index { get; set; }
        public string MainAssembly { get; set; }
        public string MachineName { get; set; }
        public string ConversationId { get; set; }
        public long ThreadId { get; set; }
        public long ProcessId { get; set; }
        public string ArchiveRecordId { get; set; }
    }
}