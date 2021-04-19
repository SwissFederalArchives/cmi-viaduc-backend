using System.Collections.Generic;
using System.Linq;

namespace CMI.Web.Frontend.api
{
    public class SynonymGroup
    {
        public SynonymGroup(List<string> entries, Source source)
        {
            Entries = entries;

            Sources = new List<Source>
            {
                source
            };
        }

        public List<string> Entries { get; }

        public List<Source> Sources { get; set; }

        public bool HasSameEntriesAs(SynonymGroup other)
        {
            return Entries.Intersect(other.Entries).Count() == Entries.Count;
        }
    }
}