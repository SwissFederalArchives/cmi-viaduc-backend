
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;

namespace CMI.Tools.AnonymizeServiceMock
{
    public class AnonymizationResponse
    {
        /// <summary>
        /// Gets or Sets the anonymized values
        /// </summary>

         [JsonProperty(PropertyName = "anonymizedValues")]
        public Dictionary<string, string> AnonymizedValues { get; set; }

    }

    public class AnonymizationRequest
    {
        [JsonProperty(PropertyName = "context")]
        public string Context { get; set; }

         [JsonProperty(PropertyName = "referenceCode")]
        public string ReferenceCode { get; set; }

         [JsonProperty(PropertyName = "values")]
        public Dictionary<string, string> Values { get; set; }

         [JsonProperty(PropertyName = "options")]
        public AnonymizeOptions Options { get; set; }
    }

    public class AnonymizeOptions
    {
        [EnumDataType(typeof(AnonymizeStyle))]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "style")]
        public AnonymizeStyle Style { get; set; }
    }

    public enum AnonymizeStyle
    {
        [EnumMember(Value = "tags")]
        Tags,
        [EnumMember(Value = "blacked")]
        Blacked
    }
}