using System;
using System.Collections.Generic;
using CMI.Contract.Common;
using CMI.Web.Frontend.api.Search;
using Nest;
using Newtonsoft.Json.Linq;

namespace CMI.Web.Frontend.api.Elastic
{
    public class ElasticQueryResult<T> where T : TreeRecord
    {
        public ISearchResponse<T> Response { get; set; }
        public Exception Exception { get; set; }

        public int Status { get; set; }
        public long TimeInMilliseconds { get; set; }

        public int TotalNumberOfHits { get; set; }

        public EntityResult<T> Data { get; set; }
        public JObject Facets { get; set; }

        public List<Entity<T>> Entries => Data?.Items;

        internal string RequestInfo { get; set; }
        internal string RequestRaw { get; set; }
        internal string ResponseRaw { get; set; }

        public bool EnableExplanations { get; set; }
    }
}