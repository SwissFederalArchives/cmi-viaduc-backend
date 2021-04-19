using System;
using System.Dynamic;

namespace CMI.Contract.Common
{
    public class ElasticRawLogRecord
    {
        public string Id { get; set; }
        public string Index { get; set; }
        public DateTime Timestamp { get; set; }
        public string Exception { get; set; }
        public string Level { get; set; }
        public string MessageTemplate { get; set; }
        public ExpandoObject Properties { get; set; }
    }
}