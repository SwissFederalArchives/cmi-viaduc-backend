using System.Collections.Generic;
using Newtonsoft.Json;

namespace CMI.Access.Sql.Viaduc
{
    public class FavoriteList
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Comment { get; set; }
        public int NumberOfItems { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<IFavorite> Items { get; set; }
    }
}