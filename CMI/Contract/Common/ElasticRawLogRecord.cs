using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CMI.Contract.Common
{
    public class ElasticRawLogRecord
    {
        [JsonPropertyName("_id")]
        public string Id { get; set; }
        [JsonPropertyName("_index")]
        public string Index { get; set; }
        [JsonPropertyName("@timestamp")]
        public DateTime InternalTimestamp { get; set; }
        [JsonPropertyName("Timestamp")]
        public DateTime Timestamp { get; set; }
        [JsonPropertyName("Level")]
        public string Level { get; set; }
        [JsonPropertyName("Properties")]
        public Dictionary<string, JsonElement> Properties { get; set; }
        [JsonPropertyName("Exception")] 
        public string Exception { get; set; }
        [JsonPropertyName("MessageTemplate")]
        public string MessageTemplate { get; set; }
    }
    
}