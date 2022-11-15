using System.Collections.Generic;

namespace CMI.Contract.Common.Entities
{
    public class CollectionItemResult
    {
        public CollectionDto Item { get; set; }
        public Dictionary<int, string> Breadcrumb { get; set; }
    }
}