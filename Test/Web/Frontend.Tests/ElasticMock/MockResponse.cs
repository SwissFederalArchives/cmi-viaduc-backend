using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CMI.Contract.Common;
using Elasticsearch.Net;
using Nest;

namespace CMI.Web.Frontend.API.Tests.ElasticMock
{
    public class MockResponse : ISearchResponse<TreeRecord>
    {
        public MockResponse(IEnumerable<IHit<TreeRecord>> hits,
            long took, long total, long maxScore)
        {
            Hits = new ReadOnlyCollection<IHit<TreeRecord>>(hits.ToList());
            Took = took;
            Total = total;
            MaxScore = maxScore;
        }

        public bool TryGetServerErrorReason(out string reason)
        {
            reason = null;
            return false;
        }

        public IApiCallDetails ApiCall { get; set; }
        public bool IsValid { get; }
        public ServerError ServerError { get; }
        public Exception OriginalException { get; }
        public string DebugInformation { get; }
        public ShardStatistics Shards { get; }
        public HitsMetadata<TreeRecord> HitsMetadata { get; }
        public AggregateDictionary Aggregations { get; }
        public Profile Profile { get; }
        public AggregateDictionary Aggs { get; }
        public SuggestDictionary<TreeRecord> Suggest { get; }
        public long Took { get; }
        public bool TimedOut { get; }
        public bool TerminatedEarly { get; }
        public string ScrollId { get; }
        public long Total { get; }
        public double MaxScore { get; }
        public long NumberOfReducePhases { get; }
        public IReadOnlyCollection<TreeRecord> Documents { get; }
        public IReadOnlyCollection<IHit<TreeRecord>> Hits { get; }
        public IReadOnlyCollection<FieldValues> Fields { get; }

        #region Nach Update 7.12.1

        ClusterStatistics ISearchResponse<TreeRecord>.Clusters => throw new NotImplementedException();

        IHitsMetadata<TreeRecord> ISearchResponse<TreeRecord>.HitsMetadata => throw new NotImplementedException();

        ISuggestDictionary<TreeRecord> ISearchResponse<TreeRecord>.Suggest => throw new NotImplementedException();

        string ISearchResponse<TreeRecord>.PointInTimeId => throw new NotImplementedException();


        #endregion
    }
}