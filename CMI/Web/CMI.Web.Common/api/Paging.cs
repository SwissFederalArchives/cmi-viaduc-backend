using System.ComponentModel;
using Newtonsoft.Json;

namespace CMI.Web.Common.api
{
    /// <summary>
    ///     Angaben für das Paging inklusive Sortierung
    /// </summary>
    public class Paging
    {
        [JsonProperty("total")]
        [ReadOnly(true)]
        public int? Total { get; set; }

        /// <summary>Elastic Feld nach dem sortiert werden soll. Dieses Property kann leer gelassen werden.</summary>
        [JsonProperty("orderBy")]
        public string OrderBy { get; set; }

        /// <summary>Hier kann "Ascending", "Descending" oder auch nichts mitgegeben werden.</summary>
        [JsonProperty("sortOrder")]
        public string SortOrder { get; set; }

        [JsonProperty("skip")] public int? Skip { get; set; }

        [JsonProperty("take")] public int? Take { get; set; }

        [JsonIgnore] public int NumberToSkip => Skip.GetValueOrDefault(-1);

        [JsonIgnore] public int NumberToTake => Take.GetValueOrDefault(-1);
    }
}