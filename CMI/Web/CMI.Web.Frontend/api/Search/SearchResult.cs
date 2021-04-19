using System.Collections.Generic;
using CMI.Contract.Common;
using CMI.Web.Common.api;
using CMI.Web.Common.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Frontend.api.Search
{
    public interface ISearchResult
    {
    }

    public class ErrorSearchResult : ISearchResult
    {
        public ApiError Error { get; set; }
    }

    public class SearchResult<T> : ISearchResult where T : TreeRecord
    {
        public EntityResult<T> Entities { get; set; }
        public JObject Facets { get; set; }
        public bool EnableExplanations { get; set; }

        public long SearchTimeInMilliseconds { get; set; }
        public long ExecutionTimeInMilliseconds { get; set; }
    }

    public class EntityResult<T> where T : TreeRecord
    {
        public List<Entity<T>> Items { get; set; }
        public Paging Paging { get; set; }
    }

    public class Entity<T> where T : TreeRecord
    {
        /// <summary>
        ///     Bewirkt, dass die Properties von T beim Serialisieren zu JSON als direkte Properties dieser Klasse angelegt werden
        /// </summary>
        [JsonExtensionData]
        public JObject ExtensionData => JObject.FromObject(Data);

        [JsonIgnore] public T Data { get; set; }

        public JObject Highlight { get; set; }
        public JObject Explanation { get; set; }
        public int Depth { get; set; }

        [JsonProperty("_metadata")] public JObject MetaData { get; set; }

        [JsonProperty("_context")] public JObject Context { get; set; }

        public bool IsDownloadAllowed { get; set; }
    }
}